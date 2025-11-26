using MarcaAi.Backend.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MarcaAi.Backend.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // ✅ DbSets
        public DbSet<ClienteMaster> ClientesMaster { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Funcionario> Funcionarios { get; set; }
        public DbSet<Servico> Servicos { get; set; }
        public DbSet<FuncionarioServico> FuncionariosServicos { get; set; }
        public DbSet<Agendamento> Agendamentos { get; set; }
        public DbSet<Disponibilidade> Disponibilidades { get; set; }
        public DbSet<Feriado> Feriados { get; set; }
        public DbSet<Bloqueio> Bloqueios { get; set; }
        public DbSet<Produto> Produtos { get; set; }
        public DbSet<MovimentacaoEstoque> MovimentacoesEstoque { get; set; }
        public DbSet<VendaProduto> VendasProdutos { get; set; }
        public DbSet<SolicitacaoExclusao> SolicitacoesExclusao { get; set; }
        public DbSet<SolicitacaoResetSenha> SolicitacoesResetSenha { get; set; }
        public DbSet<ConfiguracaoCores> ConfiguracoesCores { get; set; }
        public DbSet<AdministradorGeral> AdministradoresGerais { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ✅ Índices para Login (celular único)
            modelBuilder.Entity<Funcionario>()
                .HasIndex(f => f.Celular)
                .IsUnique();

            modelBuilder.Entity<ClienteMaster>()
                .HasIndex(c => c.Celular)
                .IsUnique();

            modelBuilder.Entity<AdministradorGeral>()
                .HasIndex(a => a.Celular)
                .IsUnique();

            // ✅ Identity Automático (opcional, mas seguro)
            modelBuilder.Entity<Cliente>().Property(c => c.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Funcionario>().Property(f => f.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<ClienteMaster>().Property(cm => cm.Id)
                .ValueGeneratedOnAdd();

            // ✅ Chaves compostas
            modelBuilder.Entity<FuncionarioServico>()
                .HasKey(fs => new { fs.FuncionarioId, fs.ServicoId });

            // ✅ Relacionamentos


            modelBuilder.Entity<Funcionario>()
                .HasOne(f => f.ClienteMaster)
                .WithMany()
                .HasForeignKey(f => f.ClienteMasterId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Servico>()
                .HasOne(s => s.ClienteMaster)
                .WithMany()
                .HasForeignKey(s => s.ClienteMasterId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FuncionarioServico>()
                .HasOne(fs => fs.Funcionario)
                .WithMany(f => f.FuncionariosServicos)
                .HasForeignKey(fs => fs.FuncionarioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FuncionarioServico>()
                .HasOne(fs => fs.Servico)
                .WithMany(s => s.FuncionariosServicos)
                .HasForeignKey(fs => fs.ServicoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Disponibilidade>()
                .HasOne(d => d.Funcionario)
                .WithMany()
                .HasForeignKey(d => d.FuncionarioId);

            modelBuilder.Entity<Bloqueio>()
                .HasOne(b => b.ClienteMaster)
                .WithMany()
                .HasForeignKey(b => b.ClienteMasterId);

            modelBuilder.Entity<Bloqueio>()
                .HasOne(b => b.Funcionario)
                .WithMany()
                .HasForeignKey(b => b.FuncionarioId);

            modelBuilder.Entity<Agendamento>()
                .HasOne(a => a.ClienteMaster)
                .WithMany()
                .HasForeignKey(a => a.ClienteMasterId);

            modelBuilder.Entity<Agendamento>()
                .HasOne(a => a.Cliente)
                .WithMany()
                .HasForeignKey(a => a.ClienteId);

            modelBuilder.Entity<Agendamento>()
                .HasOne(a => a.Servico)
                .WithMany()
                .HasForeignKey(a => a.ServicoId);

            modelBuilder.Entity<Agendamento>()
                .HasOne(a => a.Funcionario)
                .WithMany()
                .HasForeignKey(a => a.FuncionarioId);

            modelBuilder.Entity<Produto>()
                .HasOne(p => p.ClienteMaster)
                .WithMany()
                .HasForeignKey(p => p.ClienteMasterId);

            modelBuilder.Entity<MovimentacaoEstoque>()
                .HasOne(m => m.Produto)
                .WithMany()
                .HasForeignKey(m => m.ProdutoId);

            // ✅ Ajustes de tipos DateTime
            modelBuilder.Entity<Agendamento>()
                .Property(a => a.DataHora)
                .HasColumnType("timestamp without time zone");

            modelBuilder.Entity<Disponibilidade>()
                .Property(d => d.DataEspecifica)
                .HasColumnType("timestamp without time zone");

            modelBuilder.Entity<Feriado>()
                .Property(f => f.Data)
                .HasColumnType("timestamp without time zone");

            modelBuilder.Entity<MovimentacaoEstoque>()
                .Property(m => m.Data)
                .HasColumnType("timestamp without time zone");

            modelBuilder.Entity<VendaProduto>()
                .Property(v => v.DataVenda)
                .HasColumnType("timestamp without time zone");

            modelBuilder.Entity<SolicitacaoExclusao>()
                .HasOne(s => s.Agendamento)
                .WithMany() // ou .WithMany(a => a.SolicitacoesExclusao) se tiver coleção
                .HasForeignKey(s => s.AgendamentoId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ConfiguracaoCores>()
                .HasOne(cc => cc.ClienteMaster)
                .WithOne(cm => cm.ConfiguracaoCores)
                .HasForeignKey<ConfiguracaoCores>(cc => cc.ClienteMasterId);

            modelBuilder.Entity<ClienteMaster>()
                .Property(cm => cm.DataVencimento)
                .HasColumnType("timestamp without time zone");

            modelBuilder.Entity<AdministradorGeral>()
                .Property(a => a.CriadoEm)
                .HasColumnType("timestamp without time zone");
        }
    }
}
