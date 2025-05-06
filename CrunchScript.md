# CrunchScript

## Functions
| Name | Description | Parameters | Version |
| ---- | ----------- | ---------- | ------- |
| version | Specifies the minimum required version of CrunchScript. In `2.1.0+`, `latest` can be used to use the highest version supported by the current build | `latest` (`2.1.0+`) or `maj`: `number`, `min`: `number`, `pat`: `number` | `2.0.0` |
| add_package | Creates a new `.pkg` file | name: `identifier` or `string` | `2.0.0` |
| add_file | Adds a single file to a package | pkg: `identifier`, fileType: `FileType` file: `string` | `2.0.0` |
| add_folder | Adds all files in a folder to a package. Files will assumed to be `FileType::BIN` unless an associated exists | pkg: `identifier`, path: `string` or `identifier`, recursive: `bool`  | `2.0.0` |
| alias | Associates a file extension with a file type. Currently only works for `add_folder`. `add_file` requires manual type setting | pkg: `identifier`, ext: `string`, fileType: `FileType` | `2.0.0` |
| output_extension | Sets the file extension of a package. DO NOT include the dot. E.g. use `pkg` NOT `.pkg` | pkg: `identifier`, ext: `string` or `identifier` | `2.1.0` |