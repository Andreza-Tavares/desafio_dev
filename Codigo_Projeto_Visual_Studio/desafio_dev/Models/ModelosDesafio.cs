using System;
using System.Collections.Generic;

namespace desafio_dev.Models
{
    public class VendaRequest
    {
        public required List<Venda> Vendas { get; set; }
    }

    public class Venda
    {
        public required string Vendedor { get; set; }
        public decimal Valor { get; set; }
    }

    public class ProdutoEstoque
    {
        public int CodigoProduto { get; set; }
        public required string DescricaoProduto { get; set; }
        public int Estoque { get; set; }
    }

    public class MovimentacaoRequest
    {
        public int IdMovimentacao { get; set; }
        public required string TipoMovimentacao { get; set; }
        public int CodigoProduto { get; set; }
        public int Quantidade { get; set; }
    }

    public class JurosRequest
    {
        public decimal ValorOriginal { get; set; }
        public DateTime DataVencimento { get; set; }
    }
}