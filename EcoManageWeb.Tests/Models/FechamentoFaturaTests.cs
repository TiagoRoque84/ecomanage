using System;
using System.Linq;
using System.Collections.Generic;
using Xunit;
using EcoManageWeb.Models;

namespace EcoManageWeb.Tests.Models
{
    public class FechamentoFaturaTests
    {
        [Fact]
        public void Fechamento_PodeConterMultiplasPesagens_ESomarPesoLiquido()
        {
            // Arrange
            var fechamento = new FechamentoFatura
            {
                Periodo = "01/05 a 31/05",
                Pesagens = new List<EntradaCarga>
                {
                    new EntradaCarga { PesoEntrada = 10000, PesoSaida = 5000 },
                    new EntradaCarga { PesoEntrada = 12000, PesoSaida = 6000 }
                }
            };

            // Act
            var totalLiquido = fechamento.Pesagens.Sum(p => p.PesoLiquidoKg);

            // Assert
            Assert.Equal(11000m, totalLiquido); 
            // 5000 + 6000 = 11000
        }
    }
}
