using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EcoManageWeb.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using ClosedXML.Excel;
using System.IO;
using System.Collections.Generic;

namespace EcoManageWeb.Controllers
{
    [Authorize]
    public class FaturamentoController : Controller
    {
        private readonly ApplicationDbContext _db;
        public FaturamentoController(ApplicationDbContext db) { _db = db; }

        public IActionResult Index()
        {
            var faturas = _db.Fechamentos.Include(f => f.Cliente).OrderByDescending(f => f.DataLancamento).ToList();
            return View(faturas);
        }

        public IActionResult Novo(int? clienteId, DateTime? dataInicial, DateTime? dataFinal)
        {
            ViewBag.Clientes = _db.Clientes.OrderBy(c => c.RazaoSocial).ToList();
            ViewBag.ClienteSelecionado = clienteId;
            ViewBag.DataInicial = dataInicial?.ToString("yyyy-MM-dd");
            ViewBag.DataFinal = dataFinal?.ToString("yyyy-MM-dd");

            var pesagens = new List<EntradaCarga>();

            if (clienteId.HasValue)
            {
                var query = _db.Entradas
                    .Include(e => e.Veiculo).ThenInclude(v => v.Transportadora)
                    .Include(e => e.Contrato.Residuo)
                    .Include(e => e.Contrato.Cliente)
                    .Where(e => e.Contrato.ClienteId == clienteId.Value && e.Status == "Finalizada" && e.FechamentoId == null);

                if (dataInicial.HasValue) query = query.Where(e => e.DataSaida >= dataInicial.Value.Date);
                if (dataFinal.HasValue) query = query.Where(e => e.DataSaida <= dataFinal.Value.Date.AddDays(1).AddTicks(-1));

                pesagens = query.OrderBy(e => e.DataSaida).ToList();
            }

            return View(pesagens);
        }

        [HttpPost]
        public IActionResult Lancar(int clienteId, string periodoStr, List<int> pesagensSelecionadas)
        {
            if (pesagensSelecionadas == null || !pesagensSelecionadas.Any())
            {
                TempData["Error"] = "Selecione pelo menos uma pesagem para faturar.";
                return RedirectToAction("Novo", new { clienteId = clienteId });
            }

            var cliente = _db.Clientes.Find(clienteId);
            if (cliente == null) return NotFound();

            var pesagensDb = _db.Entradas
                .Include(e => e.Veiculo)
                .Include(e => e.Contrato.Residuo)
                .Include(e => e.Mtr)
                .Where(e => pesagensSelecionadas.Contains(e.Id) && e.FechamentoId == null)
                .ToList();

            if (!pesagensDb.Any()) return RedirectToAction(nameof(Index));

            var totalVolumeKg = pesagensDb.Sum(p => p.PesoLiquidoKg);
            var viagensComFrete = pesagensDb.Count(p => p.CobrarFrete);
            var totalDevido = (totalVolumeKg / 1000m * cliente.ValorTonelada) + (viagensComFrete * cliente.ValorFrete);

            var fatura = new FechamentoFatura
            {
                ClienteId = clienteId,
                Periodo = string.IsNullOrEmpty(periodoStr) ? DateTime.Now.ToString("MM/yyyy") : periodoStr,
                ValorTotal = totalDevido,
                DataLancamento = DateTime.Now,
                DataVencimento = DateTime.Now.AddDays(15),
                Status = "Rascunho"
            };

            _db.Fechamentos.Add(fatura);
            _db.SaveChanges(); // Gera o Id da fatura

            foreach(var p in pesagensDb)
            {
                p.FechamentoId = fatura.Id;
            }
            _db.SaveChanges();

            TempData["Success"] = "Faturamento gerado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult BaixarExcel(int id)
        {
            var fatura = _db.Fechamentos
                .Include(f => f.Cliente)
                .Include(f => f.Pesagens).ThenInclude(p => p.Veiculo)
                .Include(f => f.Pesagens).ThenInclude(p => p.Contrato.Residuo)
                .Include(f => f.Pesagens).ThenInclude(p => p.Mtr)
                .FirstOrDefault(f => f.Id == id);
            
            if (fatura == null) return NotFound();

            var cliente = fatura.Cliente;
            var pesagensDb = fatura.Pesagens.ToList();
            var totalVolumeKg = pesagensDb.Sum(p => p.PesoLiquidoKg);
            var viagensComFrete = pesagensDb.Count(p => p.CobrarFrete);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Faturamento");

            worksheet.Cell(1, 3).Value = $"FECHAMENTO {fatura.Periodo.ToUpper()}";
            worksheet.Cell(1, 3).Style.Font.SetBold().Font.FontSize = 14;
            worksheet.Cell(2, 3).Value = "ECOMANAGE CGR";
            worksheet.Cell(2, 3).Style.Font.SetBold().Font.FontSize = 12;

            // Info Table
            worksheet.Cell(4, 2).Value = "CLIENTE:";
            worksheet.Cell(4, 3).Value = cliente.RazaoSocial;
            worksheet.Cell(5, 2).Value = "CPF/CNPJ:";
            worksheet.Cell(5, 3).Value = cliente.CpfCnpj;
            worksheet.Cell(6, 2).Value = "PERÍODO:";
            worksheet.Cell(6, 3).Value = fatura.Periodo;
            
            // Header table
            worksheet.Cell(8, 1).Value = "DATA";
            worksheet.Cell(8, 2).Value = "PLACA";
            worksheet.Cell(8, 3).Value = "MANIFESTO";
            worksheet.Cell(8, 4).Value = "TARA";
            worksheet.Cell(8, 5).Value = "BRUTO";
            worksheet.Cell(8, 6).Value = "LIQUIDO";
            worksheet.Cell(8, 7).Value = "VALOR t.";
            worksheet.Cell(8, 8).Value = "VALOR TOTAL";
            
            worksheet.Range("A8:H8").Style.Font.SetBold().Fill.BackgroundColor = XLColor.Green;
            worksheet.Range("A8:H8").Style.Font.FontColor = XLColor.White;
            worksheet.Range("A8:H8").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            int row = 9;
            foreach (var p in pesagensDb.OrderBy(x => x.DataSaida))
            {
                worksheet.Cell(row, 1).Value = p.DataSaida?.ToString("dd/MM/yyyy");
                worksheet.Cell(row, 2).Value = p.Veiculo.Placa;
                worksheet.Cell(row, 3).Value = p.Id.ToString("D5");
                worksheet.Cell(row, 4).Value = p.PesoSaida;
                worksheet.Cell(row, 5).Value = p.PesoEntrada;
                worksheet.Cell(row, 6).Value = p.PesoLiquidoKg;
                
                worksheet.Cell(row, 7).Value = cliente.ValorTonelada;
                worksheet.Cell(row, 7).Style.NumberFormat.Format = "R$ #,##0.00";

                var valorViagem = (p.PesoLiquidoKg / 1000m * cliente.ValorTonelada) + (p.CobrarFrete ? cliente.ValorFrete : 0);
                worksheet.Cell(row, 8).Value = valorViagem;
                worksheet.Cell(row, 8).Style.NumberFormat.Format = "R$ #,##0.00";
                
                worksheet.Range(row, 1, row, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                row++;
            }

            // Totals
            row += 2;
            worksheet.Cell(row, 1).Value = "TOTAL";
            worksheet.Range(row, 1, row, 2).Merge().Style.Font.SetBold().Fill.BackgroundColor = XLColor.Green;
            worksheet.Range(row, 1, row, 2).Style.Font.FontColor = XLColor.White;
            worksheet.Range(row, 1, row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            row++;
            worksheet.Cell(row, 1).Value = "TOTAL DE VIAGENS";
            worksheet.Cell(row, 2).Value = pesagensDb.Count;
            worksheet.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            row++;
            worksheet.Cell(row, 1).Value = "TOTAL DE PESO(TON)";
            worksheet.Cell(row, 2).Value = totalVolumeKg / 1000m;
            worksheet.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            row++;
            worksheet.Cell(row, 1).Value = "FRETE";
            worksheet.Cell(row, 2).Value = viagensComFrete * cliente.ValorFrete;
            worksheet.Cell(row, 2).Style.NumberFormat.Format = "R$ #,##0.00";
            worksheet.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            row++;
            worksheet.Cell(row, 1).Value = "VALOR TOTAL";
            worksheet.Cell(row, 2).Value = fatura.ValorTotal;
            worksheet.Range(row, 1, row, 2).Style.Font.SetBold().Fill.BackgroundColor = XLColor.Green;
            worksheet.Range(row, 1, row, 2).Style.Font.FontColor = XLColor.White;
            worksheet.Cell(row, 2).Style.NumberFormat.Format = "R$ #,##0.00";
            worksheet.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            // Borders for totals
            worksheet.Range(row-4, 1, row, 2).Style.Border.OutsideBorder = XLBorderStyleValues.Thick;
            worksheet.Range(row-4, 1, row, 2).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            
            // Borders for table
            worksheet.Range(8, 1, row - 6, 8).Style.Border.OutsideBorder = XLBorderStyleValues.Thick;
            worksheet.Range(8, 1, row - 6, 8).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            var safeName = new string(cliente.RazaoSocial.Where(c => char.IsLetterOrDigit(c) || c == ' ').ToArray()).Replace(" ", "_");
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Fatura_{fatura.Id:D4}_{safeName}.xlsx");
        }

        [HttpPost]
        public IActionResult Excluir(int id)
        {
            var fatura = _db.Fechamentos.Include(f => f.Pesagens).FirstOrDefault(f => f.Id == id);
            if (fatura != null)
            {
                foreach(var p in fatura.Pesagens) {
                    p.FechamentoId = null;
                }
                _db.Fechamentos.Remove(fatura);
                _db.SaveChanges();
                TempData["Success"] = "Fatura excluída com sucesso! As pesagens voltaram para a fila.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult MarcarPago(int id)
        {
            var fatura = _db.Fechamentos.Find(id);
            if (fatura != null)
            {
                fatura.Status = "Pago";
                fatura.DataPagamento = DateTime.Now;
                _db.SaveChanges();
                TempData["Success"] = "Fatura marcada como paga!";
            }
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        public IActionResult LiberarEmLote(List<int> faturasSelecionadas)
        {
            if (faturasSelecionadas == null || !faturasSelecionadas.Any())
            {
                TempData["Error"] = "Selecione pelo menos um fechamento para liberar.";
                return RedirectToAction(nameof(Index));
            }

            var faturas = _db.Fechamentos.Where(f => faturasSelecionadas.Contains(f.Id) && f.Status == "Rascunho").ToList();
            if (!faturas.Any())
            {
                TempData["Error"] = "Nenhum rascunho válido selecionado.";
                return RedirectToAction(nameof(Index));
            }

            foreach(var fatura in faturas)
            {
                fatura.Status = "Aberto";
            }

            _db.SaveChanges();
            TempData["Success"] = $"{faturas.Count} fechamento(s) liberado(s) para os clientes!";
            return RedirectToAction(nameof(Index));
        }
    }
}
