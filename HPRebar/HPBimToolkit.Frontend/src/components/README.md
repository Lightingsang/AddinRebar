# src/components/ui

shadcn/ui components (CLI 4.21.4, style new-york), owned by this repo: edit them like any other source.
Only the Kata Export page (kata.html, kata.css) uses them. Two local changes from the registry versions:

- `bg-accent` / `bg-muted` became `bg-secondary`: in this app `accent` is the HP brand blue and `muted` is the grey text
  colour (index.css), so the shadcn meaning (a soft hover background) lives in `secondary`.
- A pressed toggle uses `bg-accent-soft text-accent-strong`.
