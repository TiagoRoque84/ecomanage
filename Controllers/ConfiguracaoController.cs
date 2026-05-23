using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EcoManageWeb.Models;
using System.Linq;

namespace EcoManageWeb.Controllers
{
    [Authorize]
    public class ConfiguracaoController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ConfiguracaoController(ApplicationDbContext db) { _db = db; }

        public IActionResult Index()
        {
            var config = _db.ConfiguracoesAterro.FirstOrDefault();
            if (config == null) {
                config = new ConfiguracaoAterro { CapacidadeDiariaKg = 450000, CapacidadeMensalKg = 9000000 };
                _db.ConfiguracoesAterro.Add(config);
                _db.SaveChanges();
            }
            return View(config);
        }

        [HttpPost]
        public IActionResult Salvar(string capacidadeDiariaKg, string capacidadeMensalKg)
        {
            var config = _db.ConfiguracoesAterro.FirstOrDefault();
            if (config != null) {
                if(!string.IsNullOrEmpty(capacidadeDiariaKg)) config.CapacidadeDiariaKg = decimal.Parse(capacidadeDiariaKg.Replace(",","."), System.Globalization.CultureInfo.InvariantCulture);
                if(!string.IsNullOrEmpty(capacidadeMensalKg)) config.CapacidadeMensalKg = decimal.Parse(capacidadeMensalKg.Replace(",","."), System.Globalization.CultureInfo.InvariantCulture);
                _db.SaveChanges();
                TempData["Success"] = "Configurações do aterro salvas com sucesso!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
