using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System;
using Microsoft.EntityFrameworkCore;
using EcoManageWeb.Models;

namespace EcoManageWeb.Controllers
{
    [Authorize]
    public class UsuariosController : Controller
    {
        private readonly ApplicationDbContext _db;
        public UsuariosController(ApplicationDbContext db) { _db = db; }

        public IActionResult Index() => View(_db.Usuarios.ToList());

        [HttpPost]
        public IActionResult Create(string nome, string email, string senha, string perfil)
        {
            try {
                var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(senha))).ToLower();
                _db.Usuarios.Add(new Usuario { Nome = nome, Email = email, SenhaHash = hash, Perfil = perfil });
                _db.SaveChanges();
            } catch (DbUpdateException) {
                TempData["Error"] = "Já existe um usuário cadastrado com este e-mail.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var item = _db.Usuarios.Find(id);
            if (item != null) { _db.Usuarios.Remove(item); _db.SaveChanges(); }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Editar(int id) => View(_db.Usuarios.Find(id));

        [HttpPost]
        public IActionResult Editar(int id, string nome, string email, string senha, string perfil, bool ativo)
        {
            try {
                var item = _db.Usuarios.Find(id);
                if(item != null) {
                    item.Nome = nome; item.Email = email; item.Perfil = perfil; item.Ativo = ativo;
                    if(!string.IsNullOrEmpty(senha)) item.SenhaHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(senha))).ToLower();
                    _db.SaveChanges();
                }
            } catch (DbUpdateException) {
                TempData["Error"] = "Já existe um usuário cadastrado com este e-mail.";
            }
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize]
    public class ClientesController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ClientesController(ApplicationDbContext db) { _db = db; }

        public IActionResult Index()
        {
            ViewBag.Residuos = _db.Residuos.ToList();
            return View(_db.Clientes.ToList());
        }

        [HttpPost]
        public IActionResult Create(string razao, string cpfCnpj, string municipio, int? residuoId, string valorTonelada, string valorFrete)
        {
            try {
                decimal pTon = 0; decimal pFrete = 0;
                if (!string.IsNullOrEmpty(valorTonelada)) pTon = decimal.Parse(valorTonelada.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                if (!string.IsNullOrEmpty(valorFrete)) pFrete = decimal.Parse(valorFrete.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);

                var cliente = new Cliente { RazaoSocial = razao, CpfCnpj = cpfCnpj, Municipio = municipio, ValorTonelada = pTon, ValorFrete = pFrete };
                _db.Clientes.Add(cliente);
                _db.SaveChanges(); // Salva para gerar o Id

                if (residuoId.HasValue && residuoId.Value > 0)
                {
                    _db.Contratos.Add(new Contrato { ClienteId = cliente.Id, ResiduoId = residuoId.Value, Status = "Ativo" });
                    _db.SaveChanges();
                }
            } catch (DbUpdateException) {
                TempData["Error"] = "Já existe um cliente cadastrado com este CPF/CNPJ.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var item = _db.Clientes.Find(id);
            if (item != null) { _db.Clientes.Remove(item); _db.SaveChanges(); }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Editar(int id) => View(_db.Clientes.Find(id));

        [HttpPost]
        public IActionResult Editar(int id, string razao, string cpfCnpj, string municipio, bool ativo, string valorTonelada, string valorFrete)
        {
            try {
                var item = _db.Clientes.Find(id);
                if(item != null) {
                    decimal pTon = 0; decimal pFrete = 0;
                    if (!string.IsNullOrEmpty(valorTonelada)) pTon = decimal.Parse(valorTonelada.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                    if (!string.IsNullOrEmpty(valorFrete)) pFrete = decimal.Parse(valorFrete.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);

                    item.RazaoSocial = razao; item.CpfCnpj = cpfCnpj; item.Municipio = municipio; item.Ativo = ativo;
                    item.ValorTonelada = pTon; item.ValorFrete = pFrete;
                    _db.SaveChanges();
                }
            } catch (DbUpdateException) {
                TempData["Error"] = "Já existe um cliente cadastrado com este CPF/CNPJ.";
            }
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize]
    public class TransportadorasController : Controller
    {
        private readonly ApplicationDbContext _db;
        public TransportadorasController(ApplicationDbContext db) { _db = db; }

        public IActionResult Index() => View(_db.Transportadoras.ToList());

        [HttpPost]
        public IActionResult Create(string nome, string cnpj)
        {
            _db.Transportadoras.Add(new Transportadora { Nome = nome, Cnpj = cnpj });
            _db.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var item = _db.Transportadoras.Find(id);
            if (item != null) { _db.Transportadoras.Remove(item); _db.SaveChanges(); }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Editar(int id) => View(_db.Transportadoras.Find(id));

        [HttpPost]
        public IActionResult Editar(int id, string nome, string cnpj)
        {
            var item = _db.Transportadoras.Find(id);
            if(item != null) {
                item.Nome = nome; item.Cnpj = cnpj;
                _db.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize]
    public class ResiduosController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ResiduosController(ApplicationDbContext db) { _db = db; }

        public IActionResult Index() => View(_db.Residuos.ToList());

        [HttpPost]
        public IActionResult Create(string nome, string classe)
        {
            _db.Residuos.Add(new Residuo { Nome = nome, Classe = classe });
            _db.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var item = _db.Residuos.Find(id);
            if (item != null) { _db.Residuos.Remove(item); _db.SaveChanges(); }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Editar(int id) => View(_db.Residuos.Find(id));

        [HttpPost]
        public IActionResult Editar(int id, string nome, string classe)
        {
            var item = _db.Residuos.Find(id);
            if(item != null) {
                item.Nome = nome; item.Classe = classe;
                _db.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize]
    public class VeiculosController : Controller
    {
        private readonly ApplicationDbContext _db;
        public VeiculosController(ApplicationDbContext db) { _db = db; }

        public IActionResult Index()
        {
            ViewBag.Transportadoras = _db.Transportadoras.ToList();
            return View(_db.Veiculos.Include(v => v.Transportadora).ToList());
        }

        [HttpPost]
        public IActionResult Create(string placa, string tara, int transportadoraId)
        {
            decimal parsedTara = 0;
            if (!string.IsNullOrEmpty(tara)) parsedTara = decimal.Parse(tara.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
            _db.Veiculos.Add(new Veiculo { Placa = placa.ToUpper(), TaraPadraoKg = parsedTara, TransportadoraId = transportadoraId });
            _db.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var item = _db.Veiculos.Find(id);
            if (item != null) { _db.Veiculos.Remove(item); _db.SaveChanges(); }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Editar(int id) {
            ViewBag.Transportadoras = _db.Transportadoras.ToList();
            return View(_db.Veiculos.Find(id));
        }

        [HttpPost]
        public IActionResult Editar(int id, string placa, string tara, int transportadoraId)
        {
            var item = _db.Veiculos.Find(id);
            if(item != null) {
                decimal parsedTara = 0;
                if (!string.IsNullOrEmpty(tara)) parsedTara = decimal.Parse(tara.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                item.Placa = placa.ToUpper(); item.TaraPadraoKg = parsedTara; item.TransportadoraId = transportadoraId;
                _db.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize]
    public class ContratosController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ContratosController(ApplicationDbContext db) { _db = db; }

        public IActionResult Index()
        {
            var contratos = _db.Contratos.Include(c => c.Cliente).Include(c => c.Residuo).ToList();
            ViewBag.Clientes = _db.Clientes.ToList();
            ViewBag.Residuos = _db.Residuos.ToList();
            return View(contratos);
        }

        [HttpPost]
        public IActionResult Create(int clienteId, int residuoId, string status)
        {
            _db.Contratos.Add(new Contrato { ClienteId = clienteId, ResiduoId = residuoId, Status = status ?? "Ativo" });
            _db.SaveChanges();
            TempData["Success"] = "Contrato registrado!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var c = _db.Contratos.Find(id);
            if(c != null)
            {
                _db.Contratos.Remove(c);
                _db.SaveChanges();
                TempData["Success"] = "Contrato excluído!";
            }
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize]
    public class MotoristasController : Controller
    {
        private readonly ApplicationDbContext _db;
        public MotoristasController(ApplicationDbContext db) { _db = db; }

        public IActionResult Index() => View(_db.Motoristas.ToList());

        [HttpPost]
        public IActionResult Create(string nome, string cpf, string cnh)
        {
            _db.Motoristas.Add(new Motorista { Nome = nome, Cpf = cpf, Cnh = cnh });
            _db.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var item = _db.Motoristas.Find(id);
            if (item != null) { _db.Motoristas.Remove(item); _db.SaveChanges(); }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Editar(int id) => View(_db.Motoristas.Find(id));

        [HttpPost]
        public IActionResult Editar(int id, string nome, string cpf, string cnh)
        {
            var item = _db.Motoristas.Find(id);
            if(item != null) {
                item.Nome = nome; item.Cpf = cpf; item.Cnh = cnh;
                _db.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
