# Semtex

Git presents all textual diffs in the same way. A change that could break your production system looks identical to a safe refactoring. Semtex splits up your diffs into those that affect the runtime behaviour and those that don't.


## Quick Start
### Make your code review's life easier
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
