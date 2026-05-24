using System;
using Xunit;
using EcoManageWeb.Models;

namespace EcoManageWeb.Tests.Models
{
    public class EntradaCargaTests
    {
        [Fact]
        public void CalcularPesoLiquido_DeveRetornarDiferencaEntreEntradaESaida()
        {
            // Arrange
            var entrada = new EntradaCarga
            {
                PesoEntrada = 15000,
                PesoSaida = 5000
            };

            // Act
            var pesoLiquido = entrada.PesoLiquidoKg;

            // Assert
            Assert.Equal(10000, pesoLiquido);
        }

        [Fact]
        public void CalcularPesoLiquido_Invertido_DeveUsarValorAbsoluto()
        {
            // O sistema usa Math.Abs para o peso líquido, evitando valores negativos.
            var entrada = new EntradaCarga
            {
                PesoEntrada = 4000,
                PesoSaida = 5000
            };

            // Act
            var pesoLiquido = entrada.PesoLiquidoKg;

            // Assert
            Assert.Equal(1000m, pesoLiquido);
        }
    }
}
