using AccountantPortal.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AccountantPortal.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Accountant> Accountants => Set<Accountant>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<NotificationMessage> NotificationMessages => Set<NotificationMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Accountant>()
            .HasOne(accountant => accountant.ApplicationUser)
            .WithOne(user => user.AccountantProfile)
            .HasForeignKey<Accountant>(accountant => accountant.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Client>()
            .HasOne(client => client.ApplicationUser)
            .WithOne(user => user.ClientProfile)
            .HasForeignKey<Client>(client => client.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Accountant>()
            .HasMany(accountant => accountant.Clients)
            .WithOne(client => client.Accountant)
            .HasForeignKey(client => client.AccountantId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Document>()
            .HasOne(document => document.Client)
            .WithMany(client => client.Documents)
            .HasForeignKey(document => document.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Document>()
            .HasOne(document => document.Accountant)
            .WithMany()
            .HasForeignKey(document => document.AccountantId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Report>()
            .HasOne(report => report.Client)
            .WithMany(client => client.Reports)
            .HasForeignKey(report => report.ClientId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Report>()
            .HasOne(report => report.Document)
            .WithMany(document => document.Reports)
            .HasForeignKey(report => report.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Report>()
            .HasOne(report => report.Accountant)
            .WithMany()
            .HasForeignKey(report => report.AccountantId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<NotificationMessage>()
            .HasOne(message => message.RecipientUser)
            .WithMany()
            .HasForeignKey(message => message.RecipientUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
