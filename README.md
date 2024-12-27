# ProjectWalker
A plugin for [PowerToys Run](https://github.com/microsoft/powertoys) that lets you navigate your local copy of repositories and perform various actions, such as opening in IDE or your browser.

**DISCLAIMER**  
This was made for my own personal use. It is far from optimised, slightly jankey, and tailored to my specific use case. The project may be expended in the future, for example with new features in the PowerToys Run API, but this should not be classed as a supported project.

If any feature suggestions / bugs are raised, I will get to them when I get time.

### Install
1. Stop PowerToys
2. Download latest .zip from the releases page
3. Drop the downloaded zip in `%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins`
4. Relaunch PowerToys

### Configuration
On first launch, a config file will be created at `%APPDATA%/ProjectWalker/config.json` with some default options. You can reload and open the config file in Notepad / VS Code by starting your search with `-c`.

The `basePath` will need to be set to the folder path where your repos live.  
*e.g. if a sample repo path is `C:\dev\project-name\repo-name`, then `basePath` should equal `C:\dev`*

### Open Options  
These are the set of options you receieve when selecting a project. 

#### Examples

**Open sln file in Rider**

```JSON
{
    "type": "process",
    "name": "Open Solution",
    "processName": "rider",
    "parameters": "{{FILE:*.sln}}"
}
```

**Copy path to clipboard**

```JSON
{
    "type": "clipboard",
    "name": "Copy Path",
    "parameters": "{{PATH}}"
}
```

**Open git remote in browser**

```JSON
{
    "type": "browser",
    "name": "Open Remote",
    "parameters": "{{GIT:REMOTE_URL}}"
}
```

**Supported Variables**  
- `{{PATH}}` - The full path to the repo folder
- `{{GIT:REMOTE_URL}}` - The remote URL for the Git repo
- `{{FILE:x}}` - Find 1st file with a specific filter (replacing `x`) e.g. `{{FILE:*.sln}}` will find the 1st .sln file. 

If a variable is used in an option, but cannot be applied (e.g. not a git repo, no suitable file found) the option will not show.