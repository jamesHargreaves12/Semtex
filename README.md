# Semtex
Development in progress -- interface may change. 
Remove the Git friction that is discouraging you from making improvements to your C# codebase.

Semtex uses semantic understanding of C# to classify changes that won't affect the semantics of your production system. This makes it easier to make improvements to your codebase without worrying about breaking anything. Plus, reviewing these changes becomes trivial!

## Features:
- Semtex is great for identifying changes that won't impact your production system. Use the following command to split your uncommitted changes into semantic and non-semantic changes:
```sh
semtex modified .
```
- If you're uncertain whether a branch will introduce a regression for your clients, Semtex can help! Use the following command to check whether a feature branch is safe:
```sh
semtex check . feature-branch
```
Or, use this more explicit command:

```sh
semtex check https://github.com/url.git feature-branch --source origin/master
```
- Not sure if Semtex is right for you? Try running it against all recent commits in your repository:
```sh
semtex check . master --all-ancestors
```
Coming soon - a command that splits a change into 2 diffs one semantic and unsemantic.
```sh
semtex split
```

## How Semtex Works:
- Semtex harnesses [Roslyn](https://github.com/dotnet/roslyn) to interact with the C# compiler's internal representation of your codebase. This produces a simplified representation of the semantics of your program, allowing us to ignore portions of your codebase that don't affect the execution, such as comments and outlined constants.

- Semtex builds on top of [Roslynator](https://github.com/JosefPihrt/Roslynator), which provides open-source code fixes for common code quality faults in C# codebases. Semtex processes all changes that won't affect the semantics of your code, making it easy to identify safe changes to make.

- Using an equality function that operates on a semantic representation rather than the text itself, Semtex can safely allow reorderings of two methods etc.

## Get Started with Semtex:
Try Semtex today by installing the tool with the following command:
```sh
dotnet tool install semtex --global
```

Happy coding! Let Semtex remove the Git friction so you can focus on improving your codebase.
