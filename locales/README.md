# Translations

Each supported UI language is a single JSON file in this folder named after its
BCP 47 language code, e.g. `en.json`, `zh-Hans.json` or `de.json`.

- `en.json` is the complete, authoritative fallback. Any key a translation
  omits (or leaves empty) falls back to English.
- Files may be partial: translate only the keys you can, and the app fills the
  rest from English.
- Metadata at the top of the file:
  - `nativeName`: the language name written in that language (used in the UI
    language list, e.g. `Deutsch` for German).
  - `wikipediaLanguage`: the Wikipedia subdomain to use for that UI language
    (e.g. `en`, `zh`; defaults to `en` if missing).

To add a language:

1. Copy `en.json` to `{code}.json` in this folder.
2. Translate the strings. You may delete the keys you have not translated yet.
3. Set `nativeName` and `wikipediaLanguage`.
4. Rebuild; the app discovers the file automatically. No code or project-file
   changes are required.

Keep the `{0}`/`{1}` placeholders intact in template strings.
