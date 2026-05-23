using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EcoManageWeb.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace EcoManageWeb.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;
        public DashboardController(ApplicationDbContext db) { _db = db; }

        public IActionResult Index()
        {
            var ultimas = _db.Entradas.Include(e => e.Contrato.Cliente).Include(e => e.Contrato.Residuo).Include(e => e.Mtr)
                            .Where(e => e.Status == "Finalizada")
                            .OrderByDescending(e => e.DataSaida).Take(10).ToList();
            return View(ultimas);
        }

        [HttpGet]
        [Route("api/indicadores")]
        public IActionResult Indicadores()
        {
            var hoje = DateTime.Now.Date;
            var mesIncial = new DateTime(hoje.Year, hoje.Month, 1);
            
            var finalizadas = _db.Entradas.Where(e => e.Status == "Finalizada").ToList();
            var finalizadasHoje = finalizadas.Where(e => e.DataSaida >= hoje).ToList();
            var finalizadasMes = finalizadas.Where(e => e.DataSaida >= mesIncial).ToList();
            
            var volumeHojeKg = finalizadasHoje.Sum(e => e.PesoLiquidoKg);
            var volumeMesKg = finalizadasMes.Sum(e => e.PesoLiquidoKg);
            
            var qtdEntradas = finalizadasHoje.Count;
            var mtrsPendentes = _db.Mtrs.Count(m => m.Status == "Emitido");

            var config = _db.ConfiguracoesAterro.FirstOrDefault() ?? new ConfiguracaoAterro { CapacidadeDiariaKg = 450_000m, CapacidadeMensalKg = 9_000_000m };

            decimal capacidadePct = 0;
            if(config.CapacidadeDiariaKg > 0) capacidadePct = Math.Round((volumeHojeKg / config.CapacidadeDiariaKg) * 100, 1);

            decimal capacidadeMesPct = 0;
            if(config.CapacidadeMensalKg > 0) capacidadeMesPct = Math.Round((volumeMesKg / config.CapacidadeMensalKg) * 100, 1);

            // Previsão Simples (Análise de Dados / ML - Etapa 7)
            int diasPassados = hoje.Day;
            decimal mediaDiariaKg = diasPassados > 0 ? (volumeMesKg / diasPassados) : 0;
            decimal previsaoFechamentoMesT = (mediaDiariaKg * 30) / 1000;

            return Json(new {
                volume_hoje_t = Math.Round(volumeHojeKg / 1000, 2),
                entradas_hoje = qtdEntradas,
                mtrs_pendentes = mtrsPendentes,
                capacidade_pct = capacidadePct,
                capacidade_mes_pct = capacidadeMesPct,
                previsao_mes_t = Math.Round(previsaoFechamentoMesT, 2)
            });
        }
    }
}
