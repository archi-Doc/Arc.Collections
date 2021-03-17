## Arc.Collection
![Build and Test](https://github.com/archi-Doc/Arc.Collection/workflows/Build%20and%20Test/badge.svg)

Work in progress...



Arc.Collection is a fast C# Collection Library which implements

- ```UnorderedList<T> ```(equivalent to ```List<T>```) : List of objects that can be accessed by index.
- ```OrderedList<T> ``` : List of objects that can be accessed by index and maintained in sorted order.
- ```OrderedSet<T>``` (equivalent to ```SortedSet<T>```) : Collection of objects that is maintained in sorted order (Red-Black Tree). It also has ```Node<T>``` access.



## Quick Start

```
Install-Package Arc.Collection
```

Sample code

```csharp
using Arc.Collection;
```

```csharp
var array = new int[] { 2, 1, 3, };
var os = new OrderedSet<int>(array);

Console.WriteLine(string.Format("{0,-12}", "Array: ") + string.Join(", ", array)); // 2, 1, 3
Console.WriteLine(string.Format("{0,-12}", "OrderedSet: ") + string.Join(", ", os)); // 1, 2, 3

Console.WriteLine("Add 4, 0");
os.Add(4);
os.Add(0);
Console.WriteLine(string.Format("{0,-12}", "OrderedSet: ") + string.Join(", ", os)); // 0, 1, 2, 3, 4
```



## Performance

```OrderedSet<T>``` uses the same tree structure (Red-Black Tree) as ```SortedSet<T>```. The difference is that ```OrderedSet<T>``` has a link to the parent node and is overall faster than ```SortedSet<T>```.

Ref: ```System.Collections.Generic.SortedSet<T>```

| Method        | Length |           Mean |       Error |       StdDev |   Gen 0 |   Gen 1 | Gen 2 | Allocated |
| ------------- | ------ | -------------: | ----------: | -----------: | ------: | ------: | ----: | --------: |
| EnumerateRef  | 10000  |       411.2 ns |     2.80 ns |      4.10 ns |  0.0916 |       - |     - |     384 B |
| Enumerate     | 10000  |       210.4 ns |     1.87 ns |      2.62 ns |  0.0229 |       - |     - |      96 B |
| NewAndAddRef  | 10000  | 1,353,968.8 ns | 7,055.84 ns |  9,419.34 ns | 82.0313 | 31.2500 |     - |  400328 B |
| NewAndAdd     | 10000  | 1,268,137.0 ns | 7,885.63 ns | 11,558.65 ns | 80.0781 | 39.0625 |     - |  480376 B |
| AddRemoveRef  | 10000  |       860.3 ns |    15.42 ns |     22.61 ns |  0.0381 |       - |     - |     160 B |
| AddRemove     | 10000  |       476.3 ns |     3.15 ns |      4.42 ns |  0.0458 |       - |     - |     192 B |
| AddRemoveNode | 10000  |       257.9 ns |     3.45 ns |      4.95 ns |  0.0458 |       - |     - |     192 B |