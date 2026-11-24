# Staff computer

During a visit, click SEARCH DATABASE to open the internal reference terminal.
Search with Enter or SEARCH. Category filters, result pages, scrolling articles,
CLEAR, CLOSE and Escape are supported. The current visit is preserved.

Edit **Staff Database.asset** in the Inspector to add or change articles. Each entry
has a title, category, search keywords and content. Categories are exactly Rulebook,
Notices or General. Use {DATE} in content for the current inspection date.
Articles are fictional local game content, with no live web connection.

The VirtualComputer component on Canvas builds the terminal UI when Play mode starts.
Its Database field selects the content asset; Checkpoint points to Passport's Decision.
Run Tools > Checkpoint > Validate Computer Database to check the starter content.
