namespace eAccountingServer.Domain.Abstractions
{
    public abstract class EntityDto
    {
        public Guid Id { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = default!;
        public Guid CreatedBy { get; set; } = default!;
        public string CreatedByName { get; set; } = default!;

        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedBy { get; set; }
        public string? UpdatedByName { get; set; }

        public DateTimeOffset? DeletedAt { get; set; }
        public Guid? DeletedBy { get; set; }

        public bool IsDeleted { get; set; }
        public bool IsActive { get; set; }
    }
}
