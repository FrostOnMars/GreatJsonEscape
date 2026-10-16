# Great JSON Escape

> Quick, that JSON is getting away! Paste escaping JSON below.

A Windows desktop app for apprehending JSON that has been escaped one too many times. It takes a backslash-riddled fugitive and returns a clean, pretty-printed confession.

## The Crime

Somewhere between your logging pipeline, your message queue, and that one legacy API, your JSON got serialized. Then serialized again. Possibly a third time, out of spite. By the time it reaches you it looks like this:

```
{\"ticket\":{\"id\":168,\"notes\":[{\"text\":\"User said: \\\"Wi-Fi isn't working.\\\"\"}]}}
```

That is not JSON. That is JSON wearing a very bad disguise, heading for the border.

## The Apprehension

Paste it in, hit **Catch it now**, and get back:

```json
{
  "ticket": {
	"id": 168,
	"notes": [
	  {
		"text": "User said: \"Wi-Fi isn't working.\""
	  }
	]
  }
}
```

Backslashes removed. Indentation restored. Dignity returned.

## Features

- **Peels escape layers recursively** — up to 5 levels deep, because some JSON really commits to the bit
- **Expands embedded JSON** — finds JSON hiding *inside* string values of other JSON and drags it into the light
- **Handles the tricky ones** — nested quotes like `\\\"Wi-Fi\\\"` and literal `\\n` sequences survive the interrogation intact
- **Pretty print or compact** — tick the box if you want it to confess quietly
- **One-click clipboard copy** — grab the results before they bolt again
- **Knows when to stop** — if it isn't actually escaped JSON, it hands it back rather than mangling it

## Requirements

- Windows
- .NET 10

## Building

```powershell
dotnet build GreatJsonEscape\GreatJsonEscape.csproj -c Release
```

## Publishing a Standalone Executable

```powershell
dotnet publish GreatJsonEscape\GreatJsonEscape.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

The result lands in `bin\Release\net10.0-windows\win-x64\publish\`. Pin it to your taskbar so you are always ready for the next escape attempt.

## Using It

1. Launch `GreatJsonEscape.exe` — double-click it, no terminal required
2. Paste your over-escaped JSON into the big box
3. Tick **Compact output** if you prefer it dense
4. Hit **Catch it now**
5. Hit **Copy to clipboard** to collect your prisoner
6. Hit **Clear** to reset the holding cell

If the input is genuinely malformed rather than merely escaped, the status bar reports *"That one got away"* along with the parser's explanation. No modal dialogs, no interruptions.

## When the JSON Is Caught

The status bar offers its congratulations, rotating through a few:

- *"Gotcha! That JSON won't be escaping on our watch."*
- *"Caught, cuffed, and pretty printed. Nice work, detective."*
- *"The great escape has been foiled. JSON is back in custody."*
- *"Slippery one, but we wrangled every last backslash."*
- *"Freedom denied. Your JSON is now fully legible and very sorry."*

## How It Works

1. **Check for a real document** — attempt an actual parse. Merely *starting* with `{` proves nothing; `{\"id\":1}` looks innocent and is not
2. **Peel one layer** — wrap the text in literal quotes so the reader *interprets* the escape sequences rather than escaping them further
3. **Repeat** — up to 5 times, stopping early if a pass changes nothing
4. **Expand stowaways** — walk the parsed tree and recursively expand any string value that is itself valid JSON, capped at 32 levels so a pathological payload cannot blow the stack
5. **Write it out** — indented by default, compact on request

## Project Layout

```
Program.cs                        Entry point
UI\MainForm.cs                    The interrogation room
Processing\JsonProcessor.cs       Unescaping logic
Processing\JsonWriterFacade.cs    Pretty printing and embedded expansion
Processing\EmbeddedJsonExpander.cs  Detects JSON hiding inside strings
Utilities\ArrayBufferWriter.cs    Output buffer
```

`JsonProcessor` has no dependency on the UI, so it can be driven directly from tests or reused elsewhere.

## Repository

https://github.com/FrostOnMars/GreatJsonEscape
