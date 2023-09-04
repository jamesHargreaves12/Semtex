# Semtex
Remove the Git friction that is discouraging you from making improvements to your C# codebase. 

Semtex aims to simplify the code review process for C# projects by intelligently categorizing your changes into those
that affect runtime behavior and those that increase the maintainability of your codebase. This is achieved by
leveraging the rich semantic information available in the C# language and compiler.

- 🏎️ <b>Accelerate Code Reviews</b>: Make the review process quicker and more focused.
- 🛠️ <b>Enables Incremental Improvements</b>: Make continual improvements without burdening your code reviewers.
- 📉 <b>Minimize Risk</b>: Lower the chances of introducing unexpected errors into your production systems.

## Quick Start
### Make your code reviews life easier
Run the following command to partition your staged changes into two parts: one containing changes that affect runtime 
behavior, and another focused on improving code quality.
```sh
semtex split
```
To commit only those changes that effect the runtime behaviour of you application use:
```sh
semtex commit Behavioural <commit message>
```
To commit changes that focus on improving quality, run:
```sh
semtex commit Quality <commit message>
```

-------

### Evaluate the Impact of a Commit
Generate a summary that highlights portions of an existing commit that impact runtime behavior:
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
codebases. Semtex processes all changes that won't affect the semantics of your code, making it easy to identify safe 
changes to make.

- Using an equality function that operates on a semantic representation rather than the text itself, Semtex can safely 
allow reorderings of two methods etc.

## Getting Started
Try Semtex today by installing the tool with the following command:
```sh
dotnet tool install semtex --global
```

----- 
Happy coding! Let Semtex remove the Git friction so you can focus on improving your codebase.

