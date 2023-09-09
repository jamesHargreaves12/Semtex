# Semtex

Remove the Git friction that is discouraging you from making improvements to your C# codebase. 

Semtex simplifies the code review process for C# projects by dividing up your changes. It segregates
diffs that affect the runtime behaviour from those that make your codebase easier to maintain. This separation 
simplifies the code review process and helps maintain the focus and intent of each commit.

## Quick Start
### Make your code review's life easier
Run the following command to split your staged changes in two: one half containing changes that affect runtime
behavior, and another focused on improving code quality.
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