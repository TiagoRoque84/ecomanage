using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EcoManageWeb.Models
{
    public class Usuario
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(120)]
        public string Nome { get; set; } = string.Empty;
        [Required, MaxLength(150)]
        public string Email { get; set; } = string.Empty;
        [Required, MaxLength(255)]
        public string SenhaHash { get; set; } = string.Empty;
        [Required, MaxLength(30)]
        public string Perfil { get; set; } = string.Empty; // Administrador, Operador, Balanceiro
        public bool Ativo { get; set; } = true;
    }

    public class Cliente
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(200)]
        public string RazaoSocial { get; set; } = string.Empty;
        [Required, MaxLength(20)]
        public string CpfCnpj { get; set; } = string.Empty;
        [MaxLength(100)]
        public string Municipio { get; set; } = string.Empty;
        public decimal ValorTonelada { get; set; } = 0;
        public decimal ValorFrete { get; set; } = 0;
        public bool Ativo { get; set; } = true;
        public ICollection<Contrato> Contratos { get; set; } = new List<Contrato>();
    }

    public class Residuo
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(150)]
        public string Nome { get; set; } = string.Empty;
        [MaxLength(50)]
        public string Classe { get; set; } = string.Empty; // Ex: Classe I, IIA, IIB
    }

    public class Transportadora
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(150)]
        public string Nome { get; set; } = string.Empty;
        [Required, MaxLength(20)]
        public string Cnpj { get; set; } = string.Empty;
    }

    public class Veiculo
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(10)]
        public string Placa { get; set; } = string.Empty;
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal TaraPadraoKg { get; set; } = 0;
        
        public int TransportadoraId { get; set; }
        [ForeignKey("TransportadoraId")]
        public Transportadora Transportadora { get; set; } = null!;
    }

    public class Contrato
    {
        [Key]
        public int Id { get; set; }
        public int ClienteId { get; set; }
        [ForeignKey("ClienteId")]
        public Cliente Cliente { get; set; } = null!;
        public int ResiduoId { get; set; }
        [ForeignKey("ResiduoId")]
        public Residuo Residuo { get; set; } = null!;
        [MaxLength(20)]
        public string Status { get; set; } = "Ativo";
    }

    public class EntradaCarga
    {
        [Key]
        public int Id { get; set; }
        public int ContratoId { get; set; }
        [ForeignKey("ContratoId")]
        public Contrato Contrato { get; set; } = null!;
        
        public int OperadorId { get; set; }
        [ForeignKey("OperadorId")]
        public Usuario Operador { get; set; } = null!;

        public int VeiculoId { get; set; }
        [ForeignKey("VeiculoId")]
        public Veiculo Veiculo { get; set; } = null!;

        [Column(TypeName = "decimal(10,2)")]
        public decimal PesoEntrada { get; set; } = 0;
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal PesoSaida { get; set; } = 0;

        [NotMapped]
        public decimal PesoLiquidoKg => Math.Abs(PesoEntrada - PesoSaida);

        public DateTime DataEntrada { get; set; } = DateTime.UtcNow;
        public DateTime? DataSaida { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "No Patio"; // No Patio, Finalizada, Cancelada
        
        public string? MotivoCancelamento { get; set; }
        public string? Observacoes { get; set; }

        public int? MotoristaId { get; set; }
        [ForeignKey("MotoristaId")]
        public Motorista? Motorista { get; set; }
        
        public MTR? Mtr { get; set; }

        public bool CobrarFrete { get; set; } = false;

        public int? FechamentoId { get; set; }
        [ForeignKey("FechamentoId")]
        public FechamentoFatura? Fechamento { get; set; }
    }

    public class FechamentoFatura
    {
        [Key]
        public int Id { get; set; }
        
        public int ClienteId { get; set; }
        [ForeignKey("ClienteId")]
        public Cliente Cliente { get; set; } = null!;
        
        [Required, MaxLength(20)]
        public string Periodo { get; set; } = string.Empty; // Ex: 01/05 a 31/05
        
        public decimal ValorTotal { get; set; }
        public DateTime DataLancamento { get; set; } = DateTime.Now;
        
        [Required, MaxLength(20)]
        public string Status { get; set; } = "Aberto"; // Aberto, Pago

        public DateTime? DataVencimento { get; set; }
        public DateTime? DataPagamento { get; set; }
        
        public ICollection<EntradaCarga> Pesagens { get; set; } = new List<EntradaCarga>();
    }

    public class MTR
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(50)]
        public string NumeroMtr { get; set; } = string.Empty;
        
        public int EntradaId { get; set; }
        [ForeignKey("EntradaId")]
        public EntradaCarga Entrada { get; set; } = null!;
        
        public DateTime EmitidoEm { get; set; } = DateTime.UtcNow;
        [MaxLength(20)]
        public string Status { get; set; } = "Emitido"; 
    }

    public class Motorista
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(150)]
        public string Nome { get; set; } = string.Empty;
        [Required, MaxLength(20)]
        public string Cpf { get; set; } = string.Empty;
        [Required, MaxLength(20)]
        public string Cnh { get; set; } = string.Empty;
    }

    public class ConfiguracaoAterro
    {
        [Key]
        public int Id { get; set; }
        public decimal CapacidadeDiariaKg { get; set; }
        public decimal CapacidadeMensalKg { get; set; }
    }

    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Transportadora> Transportadoras { get; set; }
        public DbSet<Veiculo> Veiculos { get; set; }
        public DbSet<Motorista> Motoristas { get; set; }
        public DbSet<Residuo> Residuos { get; set; }
        public DbSet<Contrato> Contratos { get; set; }
        public DbSet<EntradaCarga> Entradas { get; set; }
        public DbSet<MTR> Mtrs { get; set; }
        public DbSet<ConfiguracaoAterro> ConfiguracoesAterro { get; set; }
        public DbSet<FechamentoFatura> Fechamentos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Usuario>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<Cliente>().HasIndex(c => c.CpfCnpj).IsUnique();
            modelBuilder.Entity<Transportadora>().HasIndex(t => t.Cnpj).IsUnique();
            modelBuilder.Entity<Veiculo>().HasIndex(v => v.Placa).IsUnique();
            modelBuilder.Entity<MTR>().HasIndex(m => m.EntradaId).IsUnique();
        }
    }
}
