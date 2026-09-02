# ADR 0016: All classes should be in own file

## Status

Accepted

## Context

All classes should be contained within it's own file. No classes should be nested within another classes file. This allows better navigation of the code base by humans

## Decision

Add each class to iets own file that has the same name as the class. For instance the class
```csharp
public class human{
  
}```

Should be located int a file called
```
  human.cs
```


## Consequences

- Many more files
- Classes can be grouped by folder / namespace combination
