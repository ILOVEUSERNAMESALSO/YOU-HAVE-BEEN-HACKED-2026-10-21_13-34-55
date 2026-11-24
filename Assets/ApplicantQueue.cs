using System.Collections.Generic;

// Shuffle a whole batch: everyone visits once before anyone repeats.
public class ApplicantQueue
{
    private readonly List<CharacterCard> cards;
    private readonly System.Random random;
    private readonly List<CharacterCard> batch = new List<CharacterCard>();
    private int position;
    private CharacterCard previous;

    public ApplicantQueue(IEnumerable<CharacterCard> source, System.Random random)
    {
        this.random = random;
        cards = new List<CharacterCard>();
        foreach (var card in source)
            if (card != null && !cards.Contains(card)) cards.Add(card);
    }

    public CharacterCard Next()
    {
        if (cards.Count == 0) return null;
        if (position >= batch.Count)
        {
            batch.Clear();
            batch.AddRange(cards);
            for (int i = batch.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                var swap = batch[i]; batch[i] = batch[j]; batch[j] = swap;
            }
            if (batch.Count > 1 && batch[0] == previous)
            {
                var swap = batch[0]; batch[0] = batch[1]; batch[1] = swap;
            }
            position = 0;
        }
        previous = batch[position++];
        return previous;
    }
}
