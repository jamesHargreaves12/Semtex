# Semtex

Remove the Git friction that is discouraging you from making improvements to your C# codebase. 

Semtex segregates any changes into two categories — behavioral changes and quality improvements. This separation simplifies the code review process and helps maintain the focus and intent of each commit.

## Usage

### Make your code review's life easier
Run the following command to partition your staged changes into two patches: one for changes that affect runtime
behaviour, and another for changes focused on improving code maintainability.
```sh
semtex split
```
To commit only those changes that effect the runtime behaviour of you application use:
```sh
semtex commit Behavioural <commit message>
```
For committing changes focused on the increasing the quality of your codebase, replace Behavioral with Quality:
```sh
semtex commit Quality <commit message>
```

-------
 
### Analyze Commit Impact
To generate a summary of the parts of an existing commit that affect runtime behavior, use:
```sh
semtex check https://github.com/repo.git <commit>
```
To analyze multiple commits, you can specify a base:
```sh
semtex check https://github.com/repo.git <feature-branch> --base master
```

-----
To see a full list of available commands run:
```sh
semtex -h
```

## Licence
Semtex is licensed under the [MIT License](https://licenses.nuget.org/MIT)