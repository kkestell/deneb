# Deneb

## Usage

```console
$ deneb download --help
Description:
  Download a book

Usage:
  deneb download [options]

Options:
  --title <title> (REQUIRED)   Title
  --author <author>            Author
  --type <Fiction|NonFiction>  Type of book to download [default: Fiction]
  --interactive                Interactive mode
  -?, -h, --help               Show help and usage information
```

## Examples

```console
deneb download --title="Dune" --author="Frank Herbert" --interactive
deneb download --title="The C Programming Language" --type="NonFiction"
```
