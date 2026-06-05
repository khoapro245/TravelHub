using Microsoft.EntityFrameworkCore;

namespace TravelHub.Model
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<UserPreference> UserPreferences { get; set; } = null!;
        public DbSet<Destination> Destinations { get; set; } = null!;
        public DbSet<Itinerary> Itineraries { get; set; } = null!;
        public DbSet<ItineraryDetail> ItineraryDetails { get; set; } = null!;
        public DbSet<Budget> Budgets { get; set; } = null!;
        public DbSet<Post> Posts { get; set; } = null!;
        public DbSet<Comment> Comments { get; set; } = null!;
        public DbSet<Chat> Chats { get; set; } = null!;
        public DbSet<ChatParticipant> ChatParticipants { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;
        public DbSet<TravelCompanion> TravelCompanions { get; set; } = null!;
        public DbSet<PostLike> PostLikes { get; set; } = null!;
        public DbSet<Tour> Tours { get; set; } = null!;
        public DbSet<TourBooking> TourBookings { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 1. Users
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserID);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.RegistrationDate).HasDefaultValueSql("GETDATE()");
            });

            // 2. UserPreferences (Quan hệ 1-1 với User)
            modelBuilder.Entity<UserPreference>(entity =>
            {
                entity.HasKey(e => e.PreferenceID);
                entity.HasOne(d => d.User)
                    .WithOne(p => p.UserPreference)
                    .HasForeignKey<UserPreference>(d => d.UserID);
                entity.Property(e => e.PreferredBudgetVND).HasColumnType("decimal(18, 0)");
            });

            // 3. Destinations
            modelBuilder.Entity<Destination>(entity =>
            {
                entity.HasKey(e => e.DestinationID);
                entity.Property(e => e.EstimatedBaseCostVND).HasColumnType("decimal(18, 0)");
            });

            // 4. Itineraries
            modelBuilder.Entity<Itinerary>(entity =>
            {
                entity.HasKey(e => e.ItineraryID);
                entity.Property(e => e.Status).HasDefaultValue("Planned");
                entity.Property(e => e.TotalBudgetEstimatedVND).HasColumnType("decimal(18, 0)");
                entity.HasOne(d => d.User).WithMany(p => p.Itineraries).HasForeignKey(d => d.UserID);
            });

            // 5. ItineraryDetails
            modelBuilder.Entity<ItineraryDetail>(entity =>
            {
                entity.HasKey(e => e.DetailID);
                entity.Property(e => e.EstimatedCostVND).HasColumnType("decimal(18, 0)");
                entity.HasOne(d => d.Destination).WithMany(p => p.ItineraryDetails).HasForeignKey(d => d.DestinationID);
                entity.HasOne(d => d.Itinerary).WithMany(p => p.ItineraryDetails).HasForeignKey(d => d.ItineraryID);
            });

            // 6. Budgets
            modelBuilder.Entity<Budget>(entity =>
            {
                entity.HasKey(e => e.BudgetID);
                entity.Property(e => e.PlannedAmountVND).HasColumnType("decimal(18, 0)");
                entity.Property(e => e.ActualAmountVND).HasColumnType("decimal(18, 0)");
                entity.HasOne(d => d.Itinerary).WithMany(p => p.Budgets).HasForeignKey(d => d.ItineraryID);
            });

            // 7. Posts
            modelBuilder.Entity<Post>(entity =>
            {
                entity.HasKey(e => e.PostID);
                entity.Property(e => e.CreationDate).HasDefaultValueSql("GETDATE()");
                entity.HasOne(d => d.Itinerary).WithMany(p => p.Posts).HasForeignKey(d => d.ItineraryID).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(d => d.User).WithMany(p => p.Posts).HasForeignKey(d => d.UserID);
            });

            // 7.5 PostLikes
            modelBuilder.Entity<PostLike>(entity =>
            {
                entity.HasKey(e => new { e.UserID, e.PostID });
                entity.HasOne(d => d.User).WithMany().HasForeignKey(d => d.UserID).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(d => d.Post).WithMany().HasForeignKey(d => d.PostID).OnDelete(DeleteBehavior.Cascade);
            });

            // 8. Comments
            modelBuilder.Entity<Comment>(entity =>
            {
                entity.HasKey(e => e.CommentID);
                entity.Property(e => e.CommentDate).HasDefaultValueSql("GETDATE()");
                entity.HasOne(d => d.Post).WithMany(p => p.Comments).HasForeignKey(d => d.PostID);
                entity.HasOne(d => d.User).WithMany(p => p.Comments).HasForeignKey(d => d.UserID).OnDelete(DeleteBehavior.Restrict);
            });

            // 9. Chats
            modelBuilder.Entity<Chat>(entity =>
            {
                entity.HasKey(e => e.ChatID);
                entity.Property(e => e.IsGroupChat).HasDefaultValue(false);
                entity.Property(e => e.CreationDate).HasDefaultValueSql("GETDATE()");
            });

            // 10. ChatParticipants
            modelBuilder.Entity<ChatParticipant>(entity =>
            {
                entity.HasKey(e => e.ChatParticipantID);
                entity.HasIndex(e => new { e.ChatID, e.UserID }).IsUnique();
                entity.Property(e => e.JoinedDate).HasDefaultValueSql("GETDATE()");
                entity.HasOne(d => d.Chat).WithMany(p => p.ChatParticipants).HasForeignKey(d => d.ChatID);
                entity.HasOne(d => d.User).WithMany(p => p.ChatParticipants).HasForeignKey(d => d.UserID);
            });

            // 11. Messages
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(e => e.MessageID);
                entity.Property(e => e.SentDate).HasDefaultValueSql("GETDATE()");
                entity.HasOne(d => d.Chat).WithMany(p => p.Messages).HasForeignKey(d => d.ChatID);
                entity.HasOne(d => d.Sender).WithMany(p => p.Messages).HasForeignKey(d => d.SenderID).OnDelete(DeleteBehavior.Restrict);
            });

            // 12. TravelCompanions
            modelBuilder.Entity<TravelCompanion>(entity =>
            {
                entity.HasKey(e => e.CompanionID);
                entity.Property(e => e.DateRequested).HasDefaultValueSql("GETDATE()");
                entity.HasOne(d => d.Post).WithMany(p => p.TravelCompanions).HasForeignKey(d => d.PostID);

                // Cấu hình rõ ràng hai khóa ngoại trỏ chung về bảng Users
                entity.HasOne(d => d.Requester).WithMany(p => p.SentRequests).HasForeignKey(d => d.RequesterID).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(d => d.Receiver).WithMany(p => p.ReceivedRequests).HasForeignKey(d => d.ReceiverID).OnDelete(DeleteBehavior.Restrict);
            });

            // 13. Tours
            modelBuilder.Entity<Tour>(entity =>
            {
                entity.HasKey(e => e.TourID);
                entity.Property(e => e.PriceVND).HasColumnType("decimal(18, 2)");
            });

            // 14. TourBookings
            modelBuilder.Entity<TourBooking>(entity =>
            {
                entity.HasKey(e => e.BookingID);
                entity.Property(e => e.BookingDate).HasDefaultValueSql("GETDATE()");
                entity.HasOne(d => d.User).WithMany().HasForeignKey(d => d.UserID);
            });
        }
    }
}