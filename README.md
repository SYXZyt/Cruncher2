# Cruncher2
Cruncher is a tool for packing together multiple files into a single package file. It was primarily designed for me to use in my own game engine.
Cruncher will read a script file, written in a custom language, and produce packages based on the contents of the script.

## Usage
Cruncher is a command line tool. It will search the current working directory for a file named `Crunch.txt` and begin to execute it.
Alternatively, you can specify a different script file by passing it as the first argument to the program.

## CrunchScript
CrunchScript is the language which you will use to create packages.
Refer to [the docs](CrunchScript.md) for more information on the language.

## Getting files out
Once you have created a package, you will need to extract the files from it. I used a custom C++ system to extract files, however this is not available publicly.
I am working on a C# library for reading packages, which will be available soon. For now, you will need to write your own extractor.

You can refer to [the docs](CrunchFormat.md) for information on the package format. Bare in mind that the version of the format depends on the version of Cruncher you are using.


## Versioning
There are different versions of Cruncher, with different features and different package formats.
In your script, you will need to use `version` to specify which spec to use.
You can also use `latest` to use the highest version supported by your build of Cruncher.
The interpeter maintains backwards compatibility with older versions of the spec, so you can build older scripts with newer versions of Cruncher, and generate the older format package.