# AWITP Project Instructions

## Project Overview
This is a Lemming-style puzzle game built in Unity (C#). The game features a grid-based block system with 6 block types, inventory management, level progression, and dual Builder/Designer modes.

## Core Development Principles

### Code Quality & Simplicity
- **No Over-Engineering**: Keep solutions simple and focused. Only make changes that are directly necessary.
- **Fail Fast, Revert Faster**: When attempting fixes that don't work, REVERT those changes immediately. Don't let failed experiments bloat the codebase.
- **Question Everything**: Push back on changes you don't fully agree with. Always explain the "why" behind your suggestions and raise concerns about potential issues.

### Modularity & Future-Proofing
- **Build Systems, Not Scripts**: Always opt for modular, system-based architectures that are extensible and robust.
- **Think Ahead**: Consider where the software is headed. Build for flexibility and reusability.
- **Avoid Hard-Coding**: Use configuration, data-driven approaches, or scriptable objects instead of hard-coded values.
- **No Band-Aids**: Never apply quick fixes or workarounds without explicit acknowledgment and approval. Every solution should be proper and sustainable.

### Documentation & Maintenance
- **Document Everything**: Add clear comments and documentation, especially for complex or non-obvious code.
- **Proactive Refactoring**: When working in an area of the codebase:
  - Remove unused files and dead code
  - Update outdated patterns to match current architecture
  - Improve code structure and organization
  - Always strive to leave code better than you found it
- **Comprehensive Git Documentation**: Document all merges thoroughly with clear descriptions of what changed and why.

## Project Resources
- Technical deep-dives: See `/PROJECT.md`
- Save system architecture: See `/SAVE_SYSTEM.md`
- Auto-memory system: See `~/.claude/projects/-Users-taylorbayouth-Sites-awitp/memory/MEMORY.md`

## Key Architecture Patterns
- **Template Method**: `BaseBlock` with virtual lifecycle methods
- **Service Locator**: `ServiceRegistry` for manager access
- **Factory Pattern**: `BaseBlock.Instantiate()` for block creation
- **Event-Driven**: Inventory and progress systems use events to decouple components

## Common Commands
```bash
# Unity tests (if configured)
# Run in Unity Test Runner

# Git operations
git status
git log --oneline -10
git diff
```

## Known Technical Considerations
- Inventory UI uses event-driven updates (not per-frame polling)
- Crumbler blocks create temporary debris GameObjects (watch for GC pressure)
- No global audio mixer currently implemented
- Git LFS configured for .glb files due to GitHub's 100MB limit
