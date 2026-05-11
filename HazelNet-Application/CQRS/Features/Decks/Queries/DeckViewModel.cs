public class DeckViewModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public int TotalNumberOfCards { get; set; }
    public DateTime LastDateAccessed { get; set; }
    public DateTime CreationDate { get; set; }
    public int MasteredCards { get; set; }
    public int DueToday { get; set; }
    public DateTime? EarliestDueDate { get; set; }

    public int MasteredPercentage =>
        TotalNumberOfCards == 0 ? 0 :
            (int)Math.Round((double)MasteredCards / TotalNumberOfCards * 100, 1);
    
}