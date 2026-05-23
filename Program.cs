using EcoManageWeb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Cryptography;
using System.Text;
using System.IO;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=ecomanage.db"));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Dashboard}/{action=Index}/{id?}");

// Initialize DB and Seed Data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();

    if (!db.Usuarios.Any())
    {

    var pwdHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("admin123"))).ToLower();
    
    db.Usuarios.AddRange(
        new Usuario { Nome = "Admin", Email = "admin@eco.com", SenhaHash = pwdHash, Perfil = "Administrador" },
        new Usuario { Nome = "Balanca 01", Email = "balanca@eco.com", SenhaHash = pwdHash, Perfil = "Balanceiro" }
    );

    var resRsu = new Residuo { Nome = "RSU - Residuos Solidos Urbanos", Classe = "IIA" };
    var resInd = new Residuo { Nome = "Industrial", Classe = "I" };
    db.Residuos.AddRange(resRsu, resInd);

    var transp = new Transportadora { Nome = "TransAmbiental", Cnpj = "99.888.777/0001-66" };
    db.Transportadoras.Add(transp);

    var cliente1 = new Cliente { RazaoSocial = "Prefeitura de Mococa", CpfCnpj = "12.345.678/0001-90", Municipio = "Mococa" };
    var cliente2 = new Cliente { RazaoSocial = "Industria Alfa", CpfCnpj = "45.678.901/0001-23", Municipio = "Mococa" };
    db.Clientes.AddRange(cliente1, cliente2);

    db.SaveChanges(); // to generate IDs

    var v1 = new Veiculo { Placa = "ABC-1234", TaraPadraoKg = 15000, TransportadoraId = transp.Id };
    db.Veiculos.Add(v1);

        db.Contratos.AddRange(
            new Contrato { ClienteId = cliente1.Id, ResiduoId = resRsu.Id },
            new Contrato { ClienteId = cliente2.Id, ResiduoId = resInd.Id }
        );

        if (!db.ConfiguracoesAterro.Any())
        {
            db.ConfiguracoesAterro.Add(new ConfiguracaoAterro { 
                CapacidadeDiariaKg = 450000, 
                CapacidadeMensalKg = 9000000 
            });
        }

        db.SaveChanges();
    }
}

app.Run();
