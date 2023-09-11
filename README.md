# Semtex
[![NuGet](https://img.shields.io/nuget/v/semtex.svg)](https://www.nuget.org/packages/semtex/)

Git treats all textual diffs in the same way. A change that could break your production system looks identical to a safe refactoring. Semtex splits up your diffs into those that affect the runtime behaviour and those that don't. 

Segregating the changes makes the whole change easier to review. A change that is known not to impact the production system is trivial to review. A change that does impact runtime behaviour, should be kept small and focused.

- 📉 <b>Minimise Risk</b> by focusing your reviewer's attention on diffs that have the potential to introduce errors.
- 🏎️ <b>Accelerate Code Reviews</b> as it is easier to review something that you know won't impact the runtime behaviour.
- 🛠️ <b>Enables Incremental Improvements</b> to be made as they are spotted rather than waiting until after you have committed - there's a good chance you will forget! These changes are important to the next person who wants to understand this code.


Testing on a number of open-source projects indicates that you can expect over 25% of C# file changes to
contain changes that do not alter the behaviour.[1]

## Quick Start
### Make your code reviewer's life easier
Run the following command to split your staged changes in two: one half containing changes that affect runtime 
behaviour, and another focused on improving code quality.
```sh
semtex split
```
To commit only those changes that effect the runtime behaviour of your application use:
```sh
semtex commit Behavioural <commit message>
```
To commit changes that focus on improving quality, run:
```sh
semtex commit Quality <commit message>
```

-------

### Evaluate the Impact of a Commit
Generate a summary that highlights portions of an existing commit that impact runtime behaviour:
```sh
semtex check https://github.com/repo.git <commit>
```
To analyze multiple commits, you can specify a base:
```sh
semtex check https://github.com/repo.git <feature-branch> --base master
```

-----
### Additional Commands
For a comprehensive list of all available commands, run:
```sh
semtex -h
```

## How Semtex Works:
- Semtex harnesses [Roslyn](https://github.com/dotnet/roslyn) to interact with the C# compiler's internal representation of your codebase. This 
produces a simplified representation of the semantics of your program, allowing us to ignore portions of your codebase
that don't affect the execution, such as comments and outlined constants.

- Semtex builds on top of [Roslynator](https://github.com/JosefPihrt/Roslynator), which provides open-source code fixes for common code quality faults in C#
codebases. Semtex processes all changes that won't affect the semantics of your code, making it easy to identify these simplifications.

- The behavioural equality function acts on a semantic representation of the program rather than the text itself. This enables Semtex to recognise chages such as the reordering of methods and renaming of local variables.

## Getting Started
Try Semtex today by installing the tool with the following command:
```sh
dotnet tool install semtex --global
```

[1] Blog post coming soon with full details.
