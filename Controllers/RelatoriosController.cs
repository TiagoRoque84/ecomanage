using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EcoManageWeb.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;

namespace EcoManageWeb.Controllers
{
    [Authorize]
    public class RelatoriosController : Controller
    {
        private readonly ApplicationDbContext _db;
        public RelatoriosController(ApplicationDbContext db) { _db = db; }

        public IActionResult Fechamento(int? clienteId, DateTime? dataInicial, DateTime? dataFinal)
        {
            ViewBag.Clientes = _db.Clientes.OrderBy(c => c.RazaoSocial).ToList();

            if (clienteId.HasValue)
            {
                var cliente = _db.Clientes.Find(clienteId.Value);
                var query = _db.Entradas
                    .Include(e => e.Veiculo).ThenInclude(v => v.Transportadora)
                    .Include(e => e.Contrato.Residuo)
                    .Where(e => e.Contrato.ClienteId == clienteId.Value && (e.Status == "Finalizada" || e.Status == "Cancelada"));

                if (dataInicial.HasValue) query = query.Where(e => e.DataSaida >= dataInicial.Value.Date);
                if (dataFinal.HasValue) query = query.Where(e => e.DataSaida <= dataFinal.Value.Date.AddDays(1).AddTicks(-1));

                var pesagens = query.OrderBy(e => e.DataSaida).ToList();
                var pesagensValidas = pesagens.Where(p => p.Status != "Cancelada").ToList();
                var totalVolumeKg = pesagensValidas.Sum(p => p.PesoLiquidoKg);
                var totalViagens = pesagensValidas.Count;
                var totalDevido = (totalVolumeKg / 1000m * cliente.ValorTonelada) + (totalViagens * cliente.ValorFrete);

                ViewBag.ClienteSelecionado = cliente;
                ViewBag.TotalVolumeKg = totalVolumeKg;
                ViewBag.TotalViagens = totalViagens;
                ViewBag.TotalDevido = totalDevido;
                ViewBag.DataInicial = dataInicial?.ToString("yyyy-MM-dd");
                ViewBag.DataFinal = dataFinal?.ToString("yyyy-MM-dd");

                return View(pesagens);
            }

            return View(new System.Collections.Generic.List<EntradaCarga>());
        }

        public IActionResult Financeiro(DateTime? dataInicial, DateTime? dataFinal)
        {
            var query = _db.Fechamentos.Include(f => f.Cliente).AsQueryable();
            if (dataInicial.HasValue) query = query.Where(f => f.DataLancamento >= dataInicial.Value.Date);
            if (dataFinal.HasValue) query = query.Where(f => f.DataLancamento <= dataFinal.Value.Date.AddDays(1).AddTicks(-1));
            
            var faturas = query.OrderByDescending(f => f.DataLancamento).ToList();
            
            ViewBag.Aberto = faturas.Where(f => f.Status == "Aberto").Sum(f => f.ValorTotal);
            ViewBag.Recebido = faturas.Where(f => f.Status == "Pago").Sum(f => f.ValorTotal);
            ViewBag.DataInicial = dataInicial?.ToString("yyyy-MM-dd");
            ViewBag.DataFinal = dataFinal?.ToString("yyyy-MM-dd");

            return View(faturas);
        }

        [HttpGet]
        public IActionResult ImprimirFechamentoPdf(int clienteId, DateTime? dataInicial, DateTime? dataFinal)
        {
            var cliente = _db.Clientes.Find(clienteId);
            if(cliente == null) return NotFound();

            var query = _db.Entradas
                .Include(e => e.Veiculo).ThenInclude(v => v.Transportadora)
                .Include(e => e.Contrato.Residuo)
                .Where(e => e.Contrato.ClienteId == clienteId && (e.Status == "Finalizada" || e.Status == "Cancelada"));

            if (dataInicial.HasValue) query = query.Where(e => e.DataSaida >= dataInicial.Value.Date);
            if (dataFinal.HasValue) query = query.Where(e => e.DataSaida <= dataFinal.Value.Date.AddDays(1).AddTicks(-1));

            var pesagens = query.OrderBy(e => e.DataSaida).ToList();
            var pesagensValidas = pesagens.Where(p => p.Status != "Cancelada").ToList();
            var totalVolumeKg = pesagensValidas.Sum(p => p.PesoLiquidoKg);
            var totalViagens = pesagensValidas.Count;
            var totalDevido = (totalVolumeKg / 1000m * cliente.ValorTonelada) + (totalViagens * cliente.ValorFrete);

            using var stream = new System.IO.MemoryStream();
            var writer = new iText.Kernel.Pdf.PdfWriter(stream);
            var pdf = new iText.Kernel.Pdf.PdfDocument(writer);
            var document = new iText.Layout.Document(pdf);
            
            document.Add(new iText.Layout.Element.Paragraph("ECOMANAGE - Relatorio de Fechamento")
                .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                .SetFontSize(16));
            
            document.Add(new iText.Layout.Element.Paragraph($"Cliente: {cliente.RazaoSocial}\nCNPJ: {cliente.CpfCnpj}")
                .SetFontSize(12));
                
            document.Add(new iText.Layout.Element.Paragraph($"Periodo: {(dataInicial.HasValue ? dataInicial.Value.ToString("dd/MM/yyyy") : "Inicio")} ate {(dataFinal.HasValue ? dataFinal.Value.ToString("dd/MM/yyyy") : "Hoje")}")
                .SetFontSize(12));

            document.Add(new iText.Layout.Element.Paragraph("\nResumo Financeiro:")
                .SetFontSize(12));
            document.Add(new iText.Layout.Element.Paragraph($"Valor por Tonelada: R$ {cliente.ValorTonelada:N2} | Valor por Viagem (Frete): R$ {cliente.ValorFrete:N2}")
                .SetFontSize(10));
            document.Add(new iText.Layout.Element.Paragraph($"Total de Volume: {totalVolumeKg:N2} kg ({(totalVolumeKg/1000m):N2} t)")
                .SetFontSize(10));
            document.Add(new iText.Layout.Element.Paragraph($"Total de Viagens (validas): {totalViagens}")
                .SetFontSize(10));
            document.Add(new iText.Layout.Element.Paragraph($"Total Devido: R$ {totalDevido:N2}")
                .SetFontSize(12).SetFontColor(iText.Kernel.Colors.ColorConstants.BLUE));

            document.Add(new iText.Layout.Element.Paragraph("\nDetalhamento das Viagens:"));
            
            var table = new iText.Layout.Element.Table(new float[] { 2, 2, 3, 2, 2 }).UseAllAvailableWidth();
            table.AddHeaderCell("Data Saida");
            table.AddHeaderCell("Placa");
            table.AddHeaderCell("Residuo");
            table.AddHeaderCell("Status");
            table.AddHeaderCell("Peso Liq. (kg)");

            foreach(var p in pesagens) {
                table.AddCell(p.DataSaida?.ToString("dd/MM/yyyy HH:mm") ?? "-");
                table.AddCell(p.Veiculo.Placa);
                table.AddCell(p.Contrato.Residuo.Nome);
                table.AddCell(p.Status);
                table.AddCell(p.Status == "Cancelada" ? "0,00" : p.PesoLiquidoKg.ToString("N2"));
            }
            document.Add(table);
            document.Close();

            return File(stream.ToArray(), "application/pdf", $"Fechamento_Admin_{cliente.CpfCnpj.Replace(".","").Replace("-","").Replace("/","")}_{DateTime.Now:yyyyMMdd}.pdf");
        }
    }
}
