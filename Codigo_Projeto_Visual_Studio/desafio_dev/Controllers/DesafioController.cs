using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using desafio_dev.Models;

namespace desafio_dev.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DesafioController : ControllerBase
    {
        private readonly string _caminhoArquivoEstoque = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "estoque.json");
        private readonly string _caminhoArquivoVendas = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vendas.json");

        private List<ProdutoEstoque> LerEstoqueDoArquivo()
        {
            if (!System.IO.File.Exists(_caminhoArquivoEstoque))
            {
                Console.WriteLine($"[ERRO ESTOQUE] Arquivo não encontrado em: {_caminhoArquivoEstoque}");
                throw new FileNotFoundException($"O arquivo 'estoque.json' não foi encontrado no caminho: {_caminhoArquivoEstoque}");
            }

            string jsonString = System.IO.File.ReadAllText(_caminhoArquivoEstoque);
            try
            {
                using (var doc = JsonDocument.Parse(jsonString))
                {
                    var root = doc.RootElement;
                    if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("estoque", out var estoqueElement))
                    {
                        return JsonSerializer.Deserialize<List<ProdutoEstoque>>(estoqueElement.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO ESTOQUE PARSE] {ex.Message}");
            }

            return JsonSerializer.Deserialize<List<ProdutoEstoque>>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }

        private void SalvarEstoqueNoArquivo(List<ProdutoEstoque> lista)
        {
            var wrapper = new { estoque = lista };
            string jsonString = JsonSerializer.Serialize(wrapper, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(_caminhoArquivoEstoque, jsonString);
        }

        // 1. CÁLCULO DE COMISSÕES E CARREGAR VENDAS
        [HttpGet("carregar-vendas-json")]
        public IActionResult CarregarVendasJson()
        {
            Console.WriteLine($"[LOG VENDAS] Procurando arquivo em: {_caminhoArquivoVendas}");

            if (!System.IO.File.Exists(_caminhoArquivoVendas))
            {
                Console.WriteLine($"[ERRO VENDAS] Arquivo 'vendas.json' NÃO EXISTE na pasta de execução!");
                return NotFound(new { mensagem = "O arquivo 'vendas.json' não foi encontrado." });
            }

            try
            {
                string conteudo = System.IO.File.ReadAllText(_caminhoArquivoVendas);
                // Desserializa e serializa de novo para garantir que o JSON é válido antes de enviar
                using (var doc = JsonDocument.Parse(conteudo))
                {
                    var elementoRaiz = doc.RootElement.Clone();
                    return Ok(elementoRaiz);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO LEITURA VENDAS] {ex.Message}");
                return StatusCode(500, new { mensagem = $"Erro ao processar o JSON de vendas: {ex.Message}" });
            }
        }

        [HttpPost("calcular-comissoes")]
        public IActionResult CalcularComissoes([FromBody] VendaRequest dados)
        {
            if (dados?.Vendas == null || !dados.Vendas.Any())
            {
                Console.WriteLine("[ERRO COMISSÕES] A lista de vendas recebida no POST está nula ou vazia.");
                return BadRequest("A lista de vendas está vazia ou inválida.");
            }

            var resultado = dados.Vendas
                .GroupBy(v => v.Vendedor)
                .Select(grupo => new
                {
                    Vendedor = grupo.Key,
                    TotalVendasQuantidade = grupo.Count(),
                    TotalVendido = grupo.Sum(v => v.Valor),
                    
                    VendasAbaixoDe100 = grupo.Where(v => v.Valor < 100).Sum(v => v.Valor),
                    VendasEntre100And500 = grupo.Where(v => v.Valor >= 100 && v.Valor < 500).Sum(v => v.Valor),
                    VendasAPartirDe500 = grupo.Where(v => v.Valor >= 500).Sum(v => v.Valor),

                    ComissaoAbaixoDe100 = 0m,
                    ComissaoEntre100And500 = grupo.Where(v => v.Valor >= 100 && v.Valor < 500).Sum(v => v.Valor * 0.01m),
                    ComissaoAPartirDe500 = grupo.Where(v => v.Valor >= 500).Sum(v => v.Valor * 0.05m),

                    TotalComissao = grupo.Sum(v => 
                        v.Valor < 100 ? 0 : 
                        v.Valor < 500 ? v.Valor * 0.01m : 
                        v.Valor * 0.05m
                    )
                });

            return Ok(resultado);
        }

        // 2. GESTÃO DE ESTOQUE
        [HttpGet("listar-estoque")]
        public IActionResult ListarEstoque()
        {
            try
            {
                var estoqueAtual = LerEstoqueDoArquivo();
                return Ok(new { estoque = estoqueAtual });
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(new { mensagem = ex.Message });
            }
        }

        [HttpPost("movimentar-estoque")]
        public IActionResult MovimentarEstoque([FromBody] MovimentacaoRequest mov)
        {
            List<ProdutoEstoque> listaEstoque;
            try
            {
                listaEstoque = LerEstoqueDoArquivo();
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(new { mensagem = ex.Message });
            }

            var produto = listaEstoque.FirstOrDefault(p => p.CodigoProduto == mov.CodigoProduto);
            
            if (produto == null)
                return NotFound(new { mensagem = $"Produto com código {mov.CodigoProduto} não encontrado no estoque." });

            if (mov.TipoMovimentacao.Equals("ENTRADA", StringComparison.OrdinalIgnoreCase))
            {
                produto.Estoque += mov.Quantidade;
            }
            else if (mov.TipoMovimentacao.Equals("SAIDA", StringComparison.OrdinalIgnoreCase))
            {
                if (produto.Estoque < mov.Quantidade)
                    return BadRequest(new { mensagem = $"Estoque insuficiente. Estoque atual de '{produto.DescricaoProduto}': {produto.Estoque}" });
                
                produto.Estoque -= mov.Quantidade;
            }
            else
            {
                return BadRequest(new { mensagem = "Tipo de movimentação inválido. Utilize exatamente 'ENTRADA' ou 'SAIDA'." });
            }

            SalvarEstoqueNoArquivo(listaEstoque);

            return Ok(new
            {
                IdMovimentacao = mov.IdMovimentacao,
                DescricaoTipoMovimentacao = mov.TipoMovimentacao.ToUpper(),
                CodigoProduto = produto.CodigoProduto,
                DescricaoProduto = produto.DescricaoProduto,
                QuantidadeMovimentada = mov.Quantidade,
                EstoqueFinal = produto.Estoque
            });
        }
        
        // 3. CÁLCULO DE JUROS
        [HttpPost("calcular-juros")]
        public IActionResult CalcularJuros([FromBody] JurosRequest req)
        {
            DateTime dataPagamento = DateTime.Today;

            if (dataPagamento <= req.DataVencimento)
            {
                return Ok(new
                {
                    ValorOriginal = req.ValorOriginal,
                    DataVencimento = req.DataVencimento.ToString("yyyy-MM-dd"),
                    DataPagamentoHoje = dataPagamento.ToString("yyyy-MM-dd"),
                    DiasEmAtraso = 0,
                    ValorJuros = 0m,
                    ValorTotal = req.ValorOriginal,
                    Mensagem = "Título em dia ou a vencer. Sem juros."
                });
            }

            int diasAtraso = (dataPagamento - req.DataVencimento).Days;
            decimal taxaDiaria = 0.025m; // 2,5% ao dia
            decimal valorJuros = req.ValorOriginal * taxaDiaria * diasAtraso;
            decimal valorTotal = req.ValorOriginal + valorJuros;

            return Ok(new
            {
                ValorOriginal = req.ValorOriginal,
                DataVencimento = req.DataVencimento.ToString("yyyy-MM-dd"),
                DataPagamentoHoje = dataPagamento.ToString("yyyy-MM-dd"),
                DiasEmAtraso = diasAtraso,
                TaxaDiaria = "2,5%",
                ValorJuros = Math.Round(valorJuros, 2),
                ValorTotalAtualizado = Math.Round(valorTotal, 2)
            });
        }
    }
}