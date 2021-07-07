using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Configuration;

namespace WpfApp.Models
{
    public partial class DataContext : DbContext
    {
        public DataContext()
        {
        }

        public DataContext(DbContextOptions<DataContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Bus> Bus { get; set; }
        public virtual DbSet<BusDriver> BusDriver { get; set; }
        public virtual DbSet<BusRoute> BusRoute { get; set; }
        public virtual DbSet<City> City { get; set; }
        public virtual DbSet<Driver> Driver { get; set; }
        public virtual DbSet<Kassir> Kassir { get; set; }
        public virtual DbSet<Passenger> Passenger { get; set; }
        public virtual DbSet<Route> Route { get; set; }
        public virtual DbSet<RouteCity> RouteCity { get; set; }
        public virtual DbSet<Ticket> Ticket { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured) {
                string connStr = string.Format(ConfigurationManager.ConnectionStrings["DataContext"].ConnectionString,
                  App.AuthenticationProvider.Login,
                  App.AuthenticationProvider.Password,
                  (App.AuthenticationProvider.Roles != null && App.AuthenticationProvider.Roles.Length > 0)
                        ? App.AuthenticationProvider.Roles.First() : "");
                optionsBuilder.UseLazyLoadingProxies().UseFirebird(connStr);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Bus>(entity =>
            {
                entity.ToTable("Bus                            ");

                entity.HasIndex(e => e.Id)
                    .HasName("PK_Bus");

                entity.Property(e => e.InsuranceNumber)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Model)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.RegNumber)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.HasMany(t => t.BusRoutes).WithOne(r => r.Bus).HasForeignKey(r=>r.BusId).OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(t => t.BusDrivers).WithOne(r => r.Bus).HasForeignKey(r => r.BusId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<BusDriver>(entity =>
            {
                entity.ToTable("BusDriver                      ");

                entity.HasIndex(e => e.BusId)
                    .HasName("FK_BusDriver_2")
                    .IsUnique();

                entity.HasIndex(e => e.DriverId)
                    .HasName("FK_BusDriver_1")
                    .IsUnique();

                entity.HasIndex(e => e.Id)
                    .HasName("PK_BusDriver");

                entity.Property(e => e.RouteDate).HasColumnType("DATE");
            });

            modelBuilder.Entity<BusRoute>(entity =>
            {
                entity.ToTable("BusRoute                       ");

                entity.HasIndex(e => e.BusId)
                    .HasName("FK_BusRoute_1")
                    .IsUnique();

                entity.HasIndex(e => e.Id)
                    .HasName("PK_BusRoute");

                entity.HasIndex(e => e.RouteId)
                    .HasName("FK_BusRoute_2")
                    .IsUnique();

                entity.HasMany(t => t.Tickets).WithOne(r => r.BusRoute).HasForeignKey(r => r.BusRouteId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<City>(entity =>
            {
                entity.ToTable("City                           ");

                entity.HasIndex(e => e.Id)
                    .HasName("PK_City");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.HasMany(t => t.RouteCity).WithOne(r => r.City).HasForeignKey(r => r.CityId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Driver>(entity =>
            {
                entity.ToTable("Driver                         ");

                entity.HasIndex(e => e.Id)
                    .HasName("PK_Driver");

                entity.Property(e => e.DriverLicence)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Fathername)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.PassportNumber)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.PassportSeria)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Surname)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.HasMany(t => t.BusDrivers).WithOne(r => r.Driver).HasForeignKey(r => r.DriverId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Kassir>(entity =>
            {
                entity.ToTable("Kassir                         ");

                entity.HasIndex(e => e.Id)
                    .HasName("PK_Kassir");

                entity.Property(e => e.Fathername).HasMaxLength(255);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.PassportNumber)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.PassportSeria)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Surname)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.HasMany(t => t.Tickets).WithOne(r => r.Kassir).HasForeignKey(r => r.KassirId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Passenger>(entity =>
            {
                entity.ToTable("Passenger                      ");

                entity.HasIndex(e => e.Id)
                    .HasName("PK_Passenger");

                entity.Property(e => e.Fathername).HasMaxLength(255);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.PassportNumber)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.PassportSeria)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Surname)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.HasMany(t => t.Tickets).WithOne(r => r.Passenger).HasForeignKey(r => r.PassengerId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Route>(entity =>
            {
                entity.ToTable("Route                          ");

                entity.HasIndex(e => e.Id)
                    .HasName("PK_Route");

                entity.Property(e => e.EndPoint)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Hours).HasColumnType("DECIMAL(15, 1)");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.StartPoint)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.HasMany(t => t.BusRoutes).WithOne(r => r.Route).HasForeignKey(r => r.RouteId).OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(t => t.RouteCity).WithOne(r => r.Route).HasForeignKey(r => r.RouteId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RouteCity>(entity =>
            {
                entity.ToTable("RouteCity                      ");

                entity.HasIndex(e => e.CityId)
                    .HasName("FK_RouteCity_2")
                    .IsUnique();

                entity.HasIndex(e => e.Id)
                    .HasName("PK_RouteCity");

                entity.HasIndex(e => e.RouteId)
                    .HasName("FK_RouteCity_1")
                    .IsUnique();
            });

            modelBuilder.Entity<Ticket>(entity =>
            {
                entity.ToTable("Ticket                         ");

                entity.HasIndex(e => e.BusRouteId)
                    .HasName("FK_Ticket_3")
                    .IsUnique();

                entity.HasIndex(e => e.Id)
                    .HasName("PK_Ticket");

                entity.HasIndex(e => e.KassirId)
                    .HasName("FK_Ticket_2")
                    .IsUnique();

                entity.HasIndex(e => e.PassengerId)
                    .HasName("FK_Ticket_1")
                    .IsUnique();

                entity.Property(e => e.Price).HasColumnType("DECIMAL(15, 2)");

                entity.Property(e => e.TicketDate).HasColumnType("DATE");
            });
        }
    }
}
