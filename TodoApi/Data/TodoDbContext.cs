using Microsoft.EntityFrameworkCore;
using TodoApi.Models;

namespace TodoApi.Data;

public class TodoDbContext : DbContext
{
    public TodoDbContext(DbContextOptions<TodoDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<TodoList> TodoLists => Set<TodoList>();
    public DbSet<Todo> Todos => Set<Todo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(b =>
        {
            b.HasIndex(u => u.Email).IsUnique();
            b.Property(u => u.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<Group>(b =>
        {
            b.Property(g => g.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<GroupMember>(b =>
        {
            b.HasKey(x => new { x.GroupId, x.UserId });
            b.Property(x => x.JoinedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            b.HasOne(x => x.Group)
                .WithMany(g => g.Members)
                .HasForeignKey(x => x.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.User)
                .WithMany(u => u.GroupMemberships)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TodoList>(b =>
        {
            b.Property(l => l.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            b.HasIndex(l => new { l.OwnerUserId, l.Name });
            b.HasIndex(l => new { l.GroupId, l.Name });
            b.HasIndex(l => new { l.AssignedUserId, l.Id });

            b.HasOne(l => l.OwnerUser)
                .WithMany(u => u.Lists)
                .HasForeignKey(l => l.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(l => l.Group)
                .WithMany()
                .HasForeignKey(l => l.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(l => l.AssignedUser)
                .WithMany()
                .HasForeignKey(l => l.AssignedUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Todo>(b =>
        {
            b.HasIndex(t => new { t.ListId, t.Id });

            b.HasOne(t => t.List)
                .WithMany(l => l.Todos)
                .HasForeignKey(t => t.ListId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
