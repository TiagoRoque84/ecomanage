using Microsoft.AspNetCore.Mvc;
using EcoManageWeb.Models;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace EcoManageWeb.Controllers
{
    public class PortalClienteController : Controller
    {
        private readonly ApplicationDbContext _db;
        public PortalClienteController(ApplicationDbContext db) { _db = db; }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string cnpj)
        {
            if (string.IsNullOrWhiteSpace(cnpj))
            {
                TempData["Error"] = "CNPJ é obrigatório.";
                return View();
            }

            var cliente = _db.Clientes.FirstOrDefault(c => c.CpfCnpj == cnpj && c.Ativo);
            if (cliente == null)
            {
                TempData["Error"] = "CNPJ não encontrado em nossa base de clientes.";
                return View();
            }

            HttpContext.Session.SetString("ClienteCnpj", cliente.CpfCnpj);
            HttpContext.Session.SetString("ClienteNome", cliente.RazaoSocial);

            return RedirectToAction(nameof(Dashboard));
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult Dashboard(DateTime? dataInicial, DateTime? dataFinal)
        {
            var cnpj = HttpContext.Session.GetString("ClienteCnpj");
            if (string.IsNullOrEmpty(cnpj))
            {
                return RedirectToAction(nameof(Login));
            }

            var query = _db.Entradas
                .Include(e => e.Veiculo)
                .Include(e => e.Contrato.Cliente)
                .Include(e => e.Contrato.Residuo)
                .Include(e => e.Mtr)
                .Where(e => e.Contrato.Cliente.CpfCnpj == cnpj && (e.Status == "Finalizada" || e.Status == "Cancelada"));

            if (dataInicial.HasValue)
            {
                query = query.Where(e => e.DataSaida >= dataInicial.Value.Date);
            }
            if (dataFinal.HasValue)
            {
                query = query.Where(e => e.DataSaida <= dataFinal.Value.Date.AddDays(1).AddTicks(-1));
            }

            var pesagens = query.OrderByDescending(e => e.DataSaida).ToList();
            var cliente = _db.Clientes.FirstOrDefault(c => c.CpfCnpj == cnpj);

            ViewBag.ClienteNome = HttpContext.Session.GetString("ClienteNome");
            ViewBag.TotalVolumeKg = pesagens.Where(p => p.Status != "Cancelada").Sum(p => p.PesoLiquidoKg);
            ViewBag.TotalViagens = pesagens.Count(p => p.Status != "Cancelada");
            
            ViewBag.DataInicial = dataInicial?.ToString("yyyy-MM-dd");
            ViewBag.DataFinal = dataFinal?.ToString("yyyy-MM-dd");

            if (cliente != null)
            {
                ViewBag.Fechamentos = _db.Fechamentos
                                         .Where(f => f.ClienteId == cliente.Id && f.Status != "Rascunho")
                                         .OrderByDescending(f => f.DataLancamento)
                                         .ToList();
                                         
                ViewBag.ValorAberto = _db.Fechamentos.Where(f => f.ClienteId == cliente.Id && f.Status == "Aberto").Sum(f => f.ValorTotal);
                ViewBag.ValorPago = _db.Fechamentos.Where(f => f.ClienteId == cliente.Id && f.Status == "Pago").Sum(f => f.ValorTotal);
            }

            return View(pesagens);
        }

        [HttpGet]
        public IActionResult BaixarFaturaPdf(int id)
        {
            var cnpj = HttpContext.Session.GetString("ClienteCnpj");
            if (string.IsNullOrEmpty(cnpj)) return RedirectToAction(nameof(Login));

            var fatura = _db.Fechamentos
                .Include(f => f.Cliente)
                .Include(f => f.Pesagens).ThenInclude(p => p.Veiculo)
                .Include(f => f.Pesagens).ThenInclude(p => p.Contrato.Residuo)
                .FirstOrDefault(f => f.Id == id && f.Cliente.CpfCnpj == cnpj);

            if (fatura == null) return NotFound();

            using var stream = new System.IO.MemoryStream();
            var writer = new iText.Kernel.Pdf.PdfWriter(stream);
            var pdf = new iText.Kernel.Pdf.PdfDocument(writer);
            var document = new iText.Layout.Document(pdf);
            
            document.Add(new iText.Layout.Element.Paragraph("ECOMANAGE - FATURA DE SERVIÇOS")
                .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                .SetFontSize(16));
            
            document.Add(new iText.Layout.Element.Paragraph($"Cliente: {fatura.Cliente.RazaoSocial}\nCNPJ: {fatura.Cliente.CpfCnpj}")
                .SetFontSize(12));
                
            document.Add(new iText.Layout.Element.Paragraph($"Fatura Nº: {fatura.Id:D4}\nPeríodo: {fatura.Periodo}\nData de Lançamento: {fatura.DataLancamento:dd/MM/yyyy}")
                .SetFontSize(12));

            document.Add(new iText.Layout.Element.Paragraph("\nResumo Financeiro:")
                .SetFontSize(12));
            document.Add(new iText.Layout.Element.Paragraph($"Valor por Tonelada: R$ {fatura.Cliente.ValorTonelada:N2} | Valor por Viagem (Frete): R$ {fatura.Cliente.ValorFrete:N2}")
                .SetFontSize(10));
            document.Add(new iText.Layout.Element.Paragraph($"Total Devido: R$ {fatura.ValorTotal:N2}")
                .SetFontSize(14).SetFontColor(iText.Kernel.Colors.ColorConstants.BLUE));

            document.Add(new iText.Layout.Element.Paragraph("\nDetalhamento das Viagens Faturadas:").SetFontSize(10));
            
            var headerBg = iText.Kernel.Colors.ColorConstants.LIGHT_GRAY;
            var headerFg = iText.Kernel.Colors.ColorConstants.BLACK;

            var table = new iText.Layout.Element.Table(new float[] { 3, 3, 3, 2, 2, 2, 3, 3 }).UseAllAvailableWidth().SetFontSize(8);
            table.AddHeaderCell(new iText.Layout.Element.Cell().Add(new iText.Layout.Element.Paragraph("DATA")).SetBackgroundColor(headerBg).SetFontColor(headerFg));
            table.AddHeaderCell(new iText.Layout.Element.Cell().Add(new iText.Layout.Element.Paragraph("PLACA")).SetBackgroundColor(headerBg).SetFontColor(headerFg));
            table.AddHeaderCell(new iText.Layout.Element.Cell().Add(new iText.Layout.Element.Paragraph("MANIFESTO")).SetBackgroundColor(headerBg).SetFontColor(headerFg));
            table.AddHeaderCell(new iText.Layout.Element.Cell().Add(new iText.Layout.Element.Paragraph("TARA")).SetBackgroundColor(headerBg).SetFontColor(headerFg));
            table.AddHeaderCell(new iText.Layout.Element.Cell().Add(new iText.Layout.Element.Paragraph("BRUTO")).SetBackgroundColor(headerBg).SetFontColor(headerFg));
            table.AddHeaderCell(new iText.Layout.Element.Cell().Add(new iText.Layout.Element.Paragraph("LIQUIDO")).SetBackgroundColor(headerBg).SetFontColor(headerFg));
            table.AddHeaderCell(new iText.Layout.Element.Cell().Add(new iText.Layout.Element.Paragraph("VALOR t.")).SetBackgroundColor(headerBg).SetFontColor(headerFg));
            table.AddHeaderCell(new iText.Layout.Element.Cell().Add(new iText.Layout.Element.Paragraph("VALOR TOTAL")).SetBackgroundColor(headerBg).SetFontColor(headerFg));

            foreach(var p in fatura.Pesagens.OrderBy(x => x.DataSaida)) {
                table.AddCell(p.DataSaida?.ToString("dd/MM/yyyy") ?? "-");
                table.AddCell(p.Veiculo.Placa);
                table.AddCell(p.Id.ToString("D5"));
                table.AddCell(p.PesoSaida.ToString("N0"));
                table.AddCell(p.PesoEntrada.ToString("N0"));
                table.AddCell(p.PesoLiquidoKg.ToString("N0"));
                table.AddCell($"R$ {fatura.Cliente.ValorTonelada:N2}");
                var valorViagem = (p.PesoLiquidoKg / 1000m * fatura.Cliente.ValorTonelada) + (p.CobrarFrete ? fatura.Cliente.ValorFrete : 0);
                table.AddCell($"R$ {valorViagem:N2}");
            }
            document.Add(table);

            document.Add(new iText.Layout.Element.Paragraph("\n"));
            var totTable = new iText.Layout.Element.Table(new float[] { 2, 1 }).SetWidth(250).SetFontSize(10);
            totTable.AddCell(new iText.Layout.Element.Cell(1,2).Add(new iText.Layout.Element.Paragraph("TOTAL").SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)).SetBackgroundColor(headerBg).SetFontColor(headerFg));
            totTable.AddCell("TOTAL DE VIAGENS");
            totTable.AddCell(new iText.Layout.Element.Cell().Add(new iText.Layout.Element.Paragraph(fatura.Pesagens.Count.ToString())).SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT));
            totTable.AddCell("TOTAL DE PESO(TON)");
            var totalTon = fatura.Pesagens.Sum(p => p.PesoLiquidoKg) / 1000m;
            totTable.AddCell(new iText.Layout.Element.Cell().Add(new iText.Layout.Element.Paragraph(totalTon.ToString("N2"))).SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT));
            totTable.AddCell("FRETE");
            var totalFrete = fatura.Pesagens.Count(p => p.CobrarFrete) * fatura.Cliente.ValorFrete;
            totTable.AddCell(new iText.Layout.Element.Cell().Add(new iText.Layout.Element.Paragraph($"R$ {totalFrete:N2}")).SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT));
            totTable.AddCell(new iText.Layout.Element.Cell().Add(new iText.Layout.Element.Paragraph("VALOR TOTAL")).SetBackgroundColor(headerBg).SetFontColor(headerFg));
            totTable.AddCell(new iText.Layout.Element.Cell().Add(new iText.Layout.Element.Paragraph($"R$ {fatura.ValorTotal:N2}")).SetBackgroundColor(headerBg).SetFontColor(headerFg).SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT));
            document.Add(totTable);

            document.Close();

            return File(stream.ToArray(), "application/pdf", $"Fatura_{fatura.Id:D4}_{fatura.Periodo.Replace("/","-")}.pdf");
        }
    }
}
