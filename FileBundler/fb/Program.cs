using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Text.RegularExpressions;

var languageOption = new Option<string>(name: "--language", description: "languases in the bundle (use 'all' to include all code files)") { IsRequired = true }.FromAmong(new[] {
    "csharp","fsharp",
    "vb", "pwsh",
    "html","sql",
    "javascript",
    "python", "java",
    "cpp","c",
    "react", "all",});

languageOption.AddAlias("-l");
var outputOption = new Option<FileInfo>("--output", "File path and name");
outputOption.AddAlias("-o");

var noteOption = new Option<bool>("--note", "Write code as comments in the bundle file");
noteOption.AddAlias("-n");

var sortFilesOption = new Option<bool>("--sort", "sort the the files by type");
sortFilesOption.AddAlias("-s");

var removeEmptyLinesOption = new Option<bool>("--remove", "Remove empty lines from code files");
removeEmptyLinesOption.AddAlias("-r");

var authorOption = new Option<string>("--author", "Author of the bundle");
authorOption.AddAlias("-a");

var removeCommentsOption = new Option<bool>("--remove-comments", "Remove lines starting with // from the bundled code");
removeCommentsOption.AddAlias("-rc");

var bundleCommand = new Command("bundle", "my projet bundle");
bundleCommand.AddOption(languageOption);
bundleCommand.AddOption(outputOption);
bundleCommand.AddOption(noteOption);
bundleCommand.AddOption(sortFilesOption);
bundleCommand.AddOption(removeEmptyLinesOption);
bundleCommand.AddOption(authorOption);
bundleCommand.AddOption(removeCommentsOption);

bundleCommand.SetHandler((language, output, note, sort, remove, author, removeComments) =>
{
    var currentDirectory = Directory.GetCurrentDirectory();
    var filesToInclude = Directory.GetFiles(currentDirectory, ".", SearchOption.AllDirectories)
.Where(file => IsCodeFile(file, language))
.Where(file => !Path.GetDirectoryName(file).ToLower().Contains("bin") &&
   !Path.GetDirectoryName(file).ToLower().Contains("debug"));

    if (sort)
    {
        filesToInclude = filesToInclude.OrderBy(file => GetLanguageFromExtension(Path.GetExtension(file))).ToArray();
    }
    else
    {
        filesToInclude = filesToInclude.OrderBy(file => file).ToArray();
    }
    try
    {
        using (var fileStream = File.Create(output.FullName))
        {
            using (var writer = new StreamWriter(fileStream))
            {
                if (author != null)
                {
                    writer.WriteLine($"// Author: {author}");
                }
                foreach (var file in filesToInclude)
                {
                    try
                    {
                        using (var reader = File.OpenText(file))
                        {
                            string content = reader.ReadToEnd();
                            if (remove)
                            {
                                content = string.Join("\n", content.Split('\n').Where(line => !string.IsNullOrWhiteSpace(line)));
                            }
                            if (note)
                            {
                                writer.WriteLine("\n--------------------\n");
                                writer.WriteLine($"// Source: {Path.GetRelativePath(currentDirectory, file)}");
                            }
                            if (removeComments)
                            {
                                content = Regex.Replace(content, @"//.*?$|/\*.*?\*/|#.*?$", "", RegexOptions.Multiline);
                            }

                            writer.Write(content);
                            writer.WriteLine("--------------------\n");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Error processing file {file}: {ex.Message}");
                        Console.ResetColor();
                    }
                }
            }
        }

    }
    catch (DirectoryNotFoundException ex)
    {
        Console.WriteLine("file path is inValid");
    }

}, languageOption, outputOption, noteOption, sortFilesOption, removeEmptyLinesOption, authorOption, removeCommentsOption);
static bool IsCodeFile(string filePath, string selectedLanguage)
{
    if (selectedLanguage.Contains("all"))
    {
        return GetLanguageFromExtension(Path.GetExtension(filePath)) != "";
    }

    return GetLanguageFromExtension(Path.GetExtension(filePath)) == selectedLanguage;
}
static string GetLanguageFromExtension(string fileExtension)
{
    switch (fileExtension)
    {
        case ".cs":
            return "csharp";
        case ".fs":
            return "fsharp";
        case ".vb":
            return "vb";
        case ".sql":
            return "sql";
        case ".html":
            return "html";
        case ".js":
            return "javascript";
        case ".py":
            return "python";
        case ".java":
            return "java";
        case ".cpp":
            return "cpp";
        case ".c":
            return "c";
        case ".jsx":
            return "react";
        default:
            return "";
    }
}

var createRspCommand = new Command("create-rsp", "Create a response file for the bundle command");

createRspCommand.AddOption(new Option<string>("--language", "Enter languages (comma-separated, or 'all' for all files)"));
createRspCommand.AddOption(new Option<string>("--output", "Enter output file path"));
createRspCommand.AddOption(new Option<bool>("--note", "Include notes with source code paths? (true/false)"));
createRspCommand.AddOption(new Option<bool>("--sort", "Sort files by type? (true/false)"));
createRspCommand.AddOption(new Option<bool>("--remove", "Remove empty lines? (true/false)"));
createRspCommand.AddOption(new Option<string>("--author", "Enter author name (optional)"));
createRspCommand.AddOption(new Option<bool>("--remove-comments", "Remove comments lines? (true/false)"));
createRspCommand.Handler = CommandHandler.Create<string, string, bool, bool, bool, string, bool>(CreateRspFile);

void CreateRspFile(string language, string output, bool note, bool sort, bool remove, string author, bool removeComments)
{

    // Prompt the user for each option
    language = PromptForOption("Enter languages (comma-separated, or 'all' for all files): ", language);
    output = PromptForOption("Enter output file path: ", output);
    note = PromptForOption("Include notes with source code paths? (true/false): ", note);
    sort = PromptForOption("Sort files by type? (true/false): ", sort);
    remove = PromptForOption("Remove empty lines? (true/false): ", remove);
    author = PromptForOption("Enter author name (optional): ", author);
    removeComments = PromptForOption("Remove comments lines (optional): ", removeComments);
    // Create the response file
    var rspFilePath = Path.Combine(Directory.GetCurrentDirectory(), "bundle.rsp");
    using (var writer = new StreamWriter(rspFilePath))
    {
        if (!string.IsNullOrEmpty(language))
            writer.WriteLine($"--language {language}");
        if (!string.IsNullOrEmpty(output))
            writer.WriteLine($"--output {output}");
        if (note)
            writer.WriteLine($"--note {note}");
        if (sort)
            writer.WriteLine($"--sort {sort}");
        if (remove)
            writer.WriteLine($"--remove {remove}");
        if (!string.IsNullOrEmpty(author))
            writer.WriteLine($"--author {author}");
        if (removeComments)
            writer.WriteLine($"--remove-comments {removeComments}");

    }

    Console.WriteLine($"Response file created successfully: {rspFilePath}");
}

T PromptForOption<T>(string prompt, T defaultValue)
{
    while (true)
    {
        Console.Write(prompt);
        var userInput = Console.ReadLine();
        if (string.IsNullOrEmpty(userInput))
        {
            return defaultValue;
        }

        if (typeof(T) == typeof(string))
        {
            return (T)Convert.ChangeType(userInput, typeof(T));
        }

        if (typeof(T) == typeof(bool))
        {
            if (bool.TryParse(userInput, out var result))
            {
                return (T)Convert.ChangeType(result, typeof(T));
            }
        }

        Console.WriteLine("Invalid input. Please enter a valid value.");
    }
}
var rootCommand = new RootCommand("root command for file bundler in CLI");
rootCommand.AddCommand(createRspCommand);
rootCommand.AddCommand(bundleCommand);
rootCommand.InvokeAsync(args);