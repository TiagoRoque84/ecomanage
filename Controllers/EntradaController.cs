using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EcoManageWeb.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace EcoManageWeb.Controllers
{
    [Authorize]
    public class EntradaController : Controller
    {
        private readonly ApplicationDbContext _db;
        public EntradaController(ApplicationDbContext db) { _db = db; }

        public IActionResult Index()
        {
            var noPatio = _db.Entradas.Include(e => e.Veiculo).Include(e => e.Contrato.Cliente).Include(e => e.Contrato.Residuo)
                            .Where(e => e.Status == "No Patio").OrderByDescending(e => e.DataEntrada).ToList();
            
            var finalizadas = _db.Entradas.Include(e => e.Veiculo).Include(e => e.Contrato.Cliente).Include(e => e.Contrato.Residuo).Include(e => e.Mtr)
                            .Where(e => e.Status == "Finalizada").OrderByDescending(e => e.DataSaida).Take(20).ToList();
            
            ViewBag.NoPatio = noPatio;
            ViewBag.Finalizadas = finalizadas;
            return View();
        }

        [HttpGet]
        public IActionResult PesagemInicial()
        {
            ViewBag.Veiculos = _db.Veiculos.Include(v => v.Transportadora).ToList();
            ViewBag.Clientes = _db.Clientes.Where(c => c.Ativo).ToList();
            ViewBag.Motoristas = _db.Motoristas.ToList();
            return View();
        }

        [HttpPost]
        public IActionResult PesagemInicial(int veiculoId, int clienteId, int motoristaId, string pesoEntrada, string observacoes, bool cobrarFrete)
        {
            decimal parsedPeso = 0;
            if (!string.IsNullOrEmpty(pesoEntrada)) parsedPeso = decimal.Parse(pesoEntrada.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);

            var contrato = _db.Contratos.FirstOrDefault(c => c.ClienteId == clienteId);
            if (contrato == null) {
                var residuo = _db.Residuos.FirstOrDefault();
                if (residuo == null) {
                    residuo = new Residuo { Nome = "Não Especificado", Classe = "N/A" };
                    _db.Residuos.Add(residuo);
                    _db.SaveChanges();
                }
                contrato = new Contrato { ClienteId = clienteId, ResiduoId = residuo.Id, Status = "Ativo" };
                _db.Contratos.Add(contrato);
                _db.SaveChanges();
            }

            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var nova = new EntradaCarga
            {
                VeiculoId = veiculoId,
                ContratoId = contrato.Id,
                MotoristaId = motoristaId,
                OperadorId = int.Parse(userIdStr ?? "1"),
                PesoEntrada = parsedPeso,
                Observacoes = observacoes,
                CobrarFrete = cobrarFrete,
                DataEntrada = DateTime.Now,
                Status = "No Patio"
            };
            _db.Entradas.Add(nova);
            _db.SaveChanges();
            TempData["Success"] = "Pesagem Inicial registrada!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult PesagemFinal(int id)
        {
            var entrada = _db.Entradas
                             .Include(e => e.Veiculo).ThenInclude(v => v.Transportadora)
                             .Include(e => e.Contrato.Cliente)
                             .Include(e => e.Contrato.Residuo)
                             .FirstOrDefault(e => e.Id == id && e.Status == "No Patio");
            if (entrada == null) return NotFound();
            return View(entrada);
        }

        [HttpPost]
        public IActionResult PesagemFinal(int id, string pesoSaida)
        {
            var entrada = _db.Entradas.Include(e => e.Veiculo).ThenInclude(v => v.Transportadora)
                             .FirstOrDefault(e => e.Id == id && e.Status == "No Patio");
            if (entrada == null) return NotFound();

            decimal parsedPeso = 0;
            if (!string.IsNullOrEmpty(pesoSaida)) parsedPeso = decimal.Parse(pesoSaida.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);

            entrada.PesoSaida = parsedPeso;
            entrada.DataSaida = DateTime.Now;
            entrada.Status = "Finalizada";

            // Gerar MTR com número provisório
            var mtr = new MTR
            {
                NumeroMtr = "", // Será atualizado logo em seguida
                EntradaId = entrada.Id,
                Status = "Emitido",
                EmitidoEm = DateTime.Now
            };
            _db.Mtrs.Add(mtr);
            _db.SaveChanges(); // Gera o Id do MTR

            // Atualiza para o formato 5 dígitos sequencial único
            mtr.NumeroMtr = mtr.Id.ToString("D5");
            _db.SaveChanges();

            TempData["Success"] = $"Pesagem Final concluída. MTR {mtr.NumeroMtr} gerado.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult Cancelar(int id, string motivoCancelamento)
        {
            var entrada = _db.Entradas.Find(id);
            if(entrada != null)
            {
                entrada.Status = "Cancelada";
                entrada.MotivoCancelamento = motivoCancelamento;
                _db.SaveChanges();
                TempData["Success"] = "Pesagem cancelada com sucesso!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Editar(int id)
        {
            var entrada = _db.Entradas.Include(e => e.Veiculo).Include(e => e.Contrato.Cliente).FirstOrDefault(e => e.Id == id);
            if(entrada == null) return NotFound();
            return View(entrada);
        }

        [HttpPost]
        public IActionResult Editar(int id, string pesoEntrada, string pesoSaida, bool cobrarFrete)
        {
            var entrada = _db.Entradas.Find(id);
            if(entrada == null) return NotFound();

            entrada.CobrarFrete = cobrarFrete;

            if (!string.IsNullOrEmpty(pesoEntrada))
                entrada.PesoEntrada = decimal.Parse(pesoEntrada.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
            
            if (!string.IsNullOrEmpty(pesoSaida))
                entrada.PesoSaida = decimal.Parse(pesoSaida.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
            
            _db.SaveChanges();
            TempData["Success"] = "Pesagem atualizada!";
            return RedirectToAction(nameof(Index));
        }
    }
}
