# Great JSON Escape

> Quick, that JSON is getting away! Paste escaping JSON below.

A Windows desktop app for apprehending JSON that has been escaped one too many times. Bring in a backslash-riddled fugitive, walk out with a clean, pretty-printed confession.

## 🚨 The Crime

Somewhere between your logging pipeline, your message queue, and that one legacy API nobody will admit to writing, your JSON got serialized. Then serialized again. Possibly a third time, out of spite.

By the time it reaches you, it has changed its appearance and is making a run for the border:

```
{\"ticket\":{\"id\":168,\"notes\":[{\"text\":\"User said: \\\"Wi-Fi isn't working.\\\"\"}]}}
```

That is not JSON. That is JSON wearing a fake moustache and a trench coat.

## 🎯 The Apprehension

Paste it in. Hit **Catch it now**. Justice is served:

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

Backslashes confiscated. Indentation restored. Dignity returned to all parties.

## 🔦 Tools of the Trade

- **Recursive pursuit** — chases escaped JSON through up to 5 layers of disguise, because some payloads really commit to the bit
- **Finds the accomplices** — spots JSON hiding *inside* the string values of other JSON and drags it into the light
- **Handles the slippery ones** — nested quotes like `\\\"Wi-Fi\\\"` and literal `\\n` sequences survive interrogation intact
- **Pretty print or compact** — tick the box if you'd prefer the confession kept brief
- **One-click clipboard copy** — collect your prisoner before it bolts again
- **Knows when to stand down** — if the input was never escaped to begin with, it hands it back unharmed instead of roughing it up

## 📋 Requirements

- Windows
- .NET 10

## 🔨 Building the Case

```powershell
dotnet build GreatJsonEscape\GreatJsonEscape.csproj -c Release
```

## 🚔 Issuing a Standalone Badge

```powershell
dotnet publish GreatJsonEscape\GreatJsonEscape.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

Your officer reports for duty in `bin\Release\net10.0-windows\win-x64\publish\`. Pin it to the taskbar so you're never caught off guard by the next jailbreak.

## 🕵️ Conducting the Investigation

1. Launch `GreatJsonEscape.exe` with the icon
2. Paste the suspect into the big box
3. Tick **Compact output** if you want it to talk fast
4. Hit **Catch it now**
5. Hit **Copy to clipboard** to take custody
6. Hit **Clear** to empty the holding cell for the next one

If the input is genuinely malformed rather than merely escaped, the status bar reports **"That one got away"** along with the parser's account of what happened. No modal dialogs, no interruptions — just a quiet note in the case file.

## 🏆 Commendations

When the collar is made, the status bar offers its congratulations, rotating through a few:

- *"Gotcha! That JSON won't be escaping on our watch."*
- *"Caught, cuffed, and pretty printed. Nice work, detective."*
- *"The great escape has been foiled. JSON is back in custody."*
- *"Slippery one, but we wrangled every last backslash."*
- *"Freedom denied. Your JSON is now fully legible and very sorry."*

## 🔍 Standard Operating Procedure

1. **Verify identity** — attempt a real parse. Merely *starting* with `{` proves nothing; `{\"id\":1}` looks innocent and absolutely is not
2. **Remove one disguise** — wrap the text in literal quotes so the reader *interprets* the escape sequences instead of adding more
3. **Repeat the lineup** — up to 5 times, standing down early if a pass changes nothing
4. **Search for stowaways** — walk the parsed tree and recursively expand any string value that turns out to be JSON itself, capped at 32 levels so a pathological payload can't exhaust the stack
5. **File the report** — indented by default, compact on request

## 🗂️ Precinct Layout

```
Program.cs                          Dispatch
UI\MainForm.cs                      The interrogation room
Processing\JsonProcessor.cs         Unescaping and pursuit logic
Processing\JsonWriterFacade.cs      Pretty printing and stowaway expansion
Processing\EmbeddedJsonExpander.cs  Detects JSON hiding inside strings
Utilities\ArrayBufferWriter.cs      Evidence buffer
```

`JsonProcessor` carries no dependency on the UI, so it can be deployed undercover — driven straight from tests or reused in another operation entirely.

## 📍 Headquarters

https://github.com/FrostOnMars/GreatJsonEscape
