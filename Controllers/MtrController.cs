using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EcoManageWeb.Models;
using Microsoft.EntityFrameworkCore;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.Layout.Borders;
using iText.Kernel.Pdf.Canvas.Draw;
using System.IO;
using System.Linq;

namespace EcoManageWeb.Controllers
{
    [Authorize]
    public class MtrController : Controller
    {
        private readonly ApplicationDbContext _db;
        public MtrController(ApplicationDbContext db) { _db = db; }

        public IActionResult Index()
        {
            var mtrs = _db.Mtrs
                .Include(m => m.Entrada).ThenInclude(e => e.Contrato.Cliente)
                .Include(m => m.Entrada).ThenInclude(e => e.Contrato.Residuo)
                .Include(m => m.Entrada).ThenInclude(e => e.Veiculo.Transportadora)
                .Include(m => m.Entrada).ThenInclude(e => e.Motorista)
                .OrderByDescending(m => m.EmitidoEm).ToList();
            return View(mtrs);
        }

        [HttpGet]
        public IActionResult Pdf(int id)
        {
            var mtr = _db.Mtrs
                .Include(m => m.Entrada).ThenInclude(e => e.Contrato.Cliente)
                .Include(m => m.Entrada).ThenInclude(e => e.Contrato.Residuo)
                .Include(m => m.Entrada).ThenInclude(e => e.Veiculo).ThenInclude(v => v.Transportadora)
                .Include(m => m.Entrada).ThenInclude(e => e.Motorista)
                .Include(m => m.Entrada).ThenInclude(e => e.Operador)
                .FirstOrDefault(m => m.Id == id);

            if (mtr == null) return NotFound();

            using var stream = new MemoryStream();
            using var writer = new PdfWriter(stream);
            using var pdf = new PdfDocument(writer);
            var document = new Document(pdf);

            GerarVia(document, mtr, "1ª VIA");
            
            // Linha pontilhada
            document.Add(new Paragraph(new string('-', 150)).SetTextAlignment(TextAlignment.CENTER).SetFontColor(ColorConstants.GRAY));
            
            GerarVia(document, mtr, "2ª VIA", ColorConstants.BLUE);

            document.Close();

            return File(stream.ToArray(), "application/pdf", $"Manifesto_{mtr.NumeroMtr}.pdf");
        }

        private void GerarVia(Document document, MTR mtr, string via, Color color = null)
        {
            color ??= ColorConstants.BLACK;

            var tableHeader = new Table(new float[] { 1, 3, 1 }).UseAllAvailableWidth().SetBorder(Border.NO_BORDER);
            tableHeader.AddCell(new Cell().Add(new Paragraph("")).SetBorder(Border.NO_BORDER));
            tableHeader.AddCell(new Cell().Add(new Paragraph("ECOMANAGE\nMANIFESTO DE PESAGEM").SetTextAlignment(TextAlignment.CENTER).SetFontSize(14).SetFontColor(color)).SetBorder(Border.NO_BORDER));
            tableHeader.AddCell(new Cell().Add(new Paragraph($"{via}\nNº {mtr.NumeroMtr}").SetTextAlignment(TextAlignment.RIGHT).SetFontSize(10).SetFontColor(color)).SetBorder(Border.NO_BORDER));
            document.Add(tableHeader);
            document.Add(new LineSeparator(new SolidLine(1f)).SetMarginBottom(10));

            var tableInfo = new Table(new float[] { 1, 3 }).UseAllAvailableWidth().SetBorder(Border.NO_BORDER).SetFontSize(10).SetFontColor(color);
            tableInfo.AddCell(CellSemBorda("CLIENTE:", true)); tableInfo.AddCell(CellSemBorda(mtr.Entrada.Contrato.Cliente.RazaoSocial));
            tableInfo.AddCell(CellSemBorda("TRANSPORTADORA:", true)); tableInfo.AddCell(CellSemBorda(mtr.Entrada.Veiculo.Transportadora.Nome));
            tableInfo.AddCell(CellSemBorda("RESÍDUO:", true)); tableInfo.AddCell(CellSemBorda(mtr.Entrada.Contrato.Residuo.Nome));
            document.Add(tableInfo);
            document.Add(new LineSeparator(new SolidLine(1f)).SetMarginTop(5).SetMarginBottom(5));

            var tableVeic = new Table(new float[] { 1, 3 }).UseAllAvailableWidth().SetBorder(Border.NO_BORDER).SetFontSize(10).SetFontColor(color);
            tableVeic.AddCell(CellSemBorda("MOTORISTA:", true)); tableVeic.AddCell(CellSemBorda(mtr.Entrada.Motorista?.Nome ?? "N/I"));
            tableVeic.AddCell(CellSemBorda("PLACA VEÍCULO:", true)); tableVeic.AddCell(CellSemBorda(mtr.Entrada.Veiculo.Placa));
            tableVeic.AddCell(CellSemBorda("DATA/HORA ENTRADA:", true)); tableVeic.AddCell(CellSemBorda(mtr.Entrada.DataEntrada.ToString("dd/MM/yyyy HH:mm:ss")));
            tableVeic.AddCell(CellSemBorda("DATA/HORA SAÍDA:", true)); tableVeic.AddCell(CellSemBorda(mtr.Entrada.DataSaida?.ToString("dd/MM/yyyy HH:mm:ss") ?? "N/A"));
            document.Add(tableVeic);
            document.Add(new LineSeparator(new SolidLine(1f)).SetMarginTop(5).SetMarginBottom(10));

            var tablePesos = new Table(new float[] { 1, 1, 1 }).UseAllAvailableWidth().SetBorder(Border.NO_BORDER).SetFontSize(11).SetFontColor(color);
            tablePesos.AddCell(new Cell().Add(new Paragraph($"PESO ENTRADA:\n{mtr.Entrada.PesoEntrada:0.00} kg")).SetBorder(Border.NO_BORDER));
            tablePesos.AddCell(new Cell().Add(new Paragraph($"PESO SAÍDA:\n{mtr.Entrada.PesoSaida:0.00} kg")).SetBorder(Border.NO_BORDER));
            
            var liqCell = new Cell().Add(new Paragraph($"PESO LÍQUIDO:\n{mtr.Entrada.PesoLiquidoKg:0.00} kg")).SetBorder(Border.NO_BORDER);
            if (via == "1ª VIA") liqCell.SetBackgroundColor(ColorConstants.LIGHT_GRAY);
            else liqCell.SetBackgroundColor(new DeviceRgb(220, 230, 255));
            tablePesos.AddCell(liqCell);
            
            document.Add(tablePesos);
            document.Add(new LineSeparator(new SolidLine(1f)).SetMarginTop(10).SetMarginBottom(20));

            var tableAss = new Table(new float[] { 1, 1 }).UseAllAvailableWidth().SetBorder(Border.NO_BORDER).SetFontSize(9).SetFontColor(color);
            tableAss.AddCell(CellSemBorda("ASSINATURA DO MOTORISTA:\n_________________________________________\n" + (mtr.Entrada.Motorista?.Nome ?? ""), true));
            var operadorNome = mtr.Entrada.Operador?.Nome ?? "Responsável ECOMANAGE";
            tableAss.AddCell(CellSemBorda($"ASSINATURA DO RESPONSÁVEL:\n_________________________________________\n{operadorNome}", true));
            document.Add(tableAss);

            document.Add(new Paragraph(" ").SetFontSize(10).SetMarginBottom(10)); // Spacer reduced
        }

        private Cell CellSemBorda(string text, bool bold = false)
        {
            var p = new Paragraph(text);
            return new Cell().Add(p).SetBorder(Border.NO_BORDER);
        }
    }
}
