# Fork Maintenance Notes

This fork intentionally keeps a small local CLI extension for project-local AIBridge commands.

## Local Changes To Preserve

- `Tools~/AIBridgeCLI/Commands/RawCommandBuilder.cs`
- `Register(new RawCommandBuilder());` in `Tools~/AIBridgeCLI/Commands/CommandRegistry.cs`
- Updated `Tools~/CLI/win-x64/AIBridgeCLI.exe`, `.dll`, and `.pdb` when CLI source changes must be usable from the packaged fork immediately.

`RawCommandBuilder` must stay project-agnostic. Do not add GOF-specific command examples or behavior here; project-specific command descriptions belong in the Unity project-local `ICommand.SkillDescription` or the project skill.

## Merging Upstream

When merging upstream AIBridge:

1. Resolve source conflicts first.
2. Keep upstream command registrations and keep `Register(new RawCommandBuilder());`.
3. If `Tools~/CLI/win-x64/AIBridgeCLI.exe`, `.dll`, or `.pdb` conflict, do not manually merge binaries.
4. After source conflicts are resolved, run `BuildAIBridgeCLI.bat`.
5. Commit the regenerated win-x64 CLI binaries with the source changes.

If only source is updated and `Tools~/CLI/win-x64` is not regenerated, users who install the package directly will not get the new CLI command until they rebuild the CLI themselves.
