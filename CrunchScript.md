# CrunchScript

## Functions
| Name | Description | Parameters |
| ---- | ----------- | ---------- |
| version | Specifies the minimum required version of CrunchScript. | `maj`: `number`, `min`: `number`, `pat`: `number` | 
| add_package | Creates a new `.pkg` file | name: `identifier` or `string` |
| add_file | Adds a single file to a package | pkg: `identifier`, fileType: `FileType` file: `string` |
| add_folder | Adds all files in a folder to a package. Files will assumed to be `FileType::BIN` unless an associated exists | pkg: `identifier`, path: `string` or `identifier`, recursive: `bool`  |
| alias | Associates a file extension with a file type. Currently only works for `add_folder`. `add_file` requires manual type setting | pkg: `identifier`, ext: `string`, fileType: `FileType` |