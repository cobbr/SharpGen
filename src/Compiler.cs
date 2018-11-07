// Author: Ryan Cobb (@cobbr_io)
// Project: SharpGen (https://github.com/cobbr/SharpGen)
// License: BSD 3-Clause

using System;
using System.IO;
using System.Text;
using System.Linq;
using System.IO.Compression;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;

namespace SharpGen
{
    public class Compiler
    {
        public class CompilationRequest
        {
            public string Source { get; set; } = null;
            public string SourceDirectory { get; set; } = null;
            public string ResourceDirectory { get; set; } = null;
            public string ReferenceDirectory { get; set; } = null;

            public DotNetVersion TargetDotNetVersion { get; set; } = DotNetVersion.Net35;
            public OutputKind OutputKind { get; set; } = OutputKind.DynamicallyLinkedLibrary;
            public Platform Platform { get; set; } = Platform.AnyCpu;
            public bool Optimize = true;

            public string AssemblyName { get; set; } = null;
            public List<Reference> References { get; set; } = new List<Reference>();
            public List<EmbeddedResource> EmbeddedResources { get; set; } = new List<EmbeddedResource>();
        }

        public enum DotNetVersion
        {
            Net35,
            Net40
        }

        public class EmbeddedResource
        {
            public string Name { get; set; }
            public string File { get; set; }
            public Platform Platform { get; set; } = Platform.AnyCpu;
            public bool Enabled { get; set; } = false;
        }

        public class Reference
        {
            public string File { get; set; }
            public DotNetVersion Framework { get; set; } = DotNetVersion.Net35;
            public bool Enabled { get; set; } = false;
        }

        private class SourceSyntaxTree
        {
            public string FileName { get; set; } = "";
            public SyntaxTree SyntaxTree { get; set; }
            public List<INamedTypeSymbol> UsedTypes { get; set; } = new List<INamedTypeSymbol>();
        }

        public static byte[] Compile(CompilationRequest request)
        {
            // Gather SyntaxTrees for compilation
            List<SourceSyntaxTree> sourceSyntaxTrees = Directory.GetFiles(request.SourceDirectory, "*.cs", SearchOption.AllDirectories)
                .Select(F => new SourceSyntaxTree { FileName = F, SyntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(F), new CSharpParseOptions()) })
                .ToList();
            List<SyntaxTree> compilationTrees = sourceSyntaxTrees.Select(S => S.SyntaxTree).ToList();

            SyntaxTree sourceTree = CSharpSyntaxTree.ParseText(request.Source, new CSharpParseOptions());
            compilationTrees.Add(sourceTree);

            // Use specified OutputKind and Platform
            CSharpCompilationOptions options = new CSharpCompilationOptions(outputKind: request.OutputKind, optimizationLevel: OptimizationLevel.Release, platform: request.Platform);
            // Compile to obtain SemanticModel
            CSharpCompilation compilation = CSharpCompilation.Create(
                request.AssemblyName == null ? Path.GetRandomFileName() : request.AssemblyName,
                compilationTrees,
                request.References.Where(R => R.Framework == request.TargetDotNetVersion).Where(R => R.Enabled).Select(R =>
                {
                    string folder = (request.TargetDotNetVersion == DotNetVersion.Net35 ? request.ReferenceDirectory + "net35" + Path.DirectorySeparatorChar : request.ReferenceDirectory + "net40" + Path.DirectorySeparatorChar);
                    return MetadataReference.CreateFromFile(folder + R.File);
                }).ToList(),
                options
            );

            // Perform source code optimization, removing unused types
            if (request.Optimize)
            {
                // Find all Types used by the generated compilation
                List<INamedTypeSymbol> usedTypes = new List<INamedTypeSymbol>();
                GetUsedTypesRecursively(compilation, sourceTree, ref usedTypes, ref sourceSyntaxTrees);
                usedTypes = usedTypes.Distinct().ToList();
                List<string> usedTypeNames = usedTypes.Select(T => GetFullyQualifiedTypeName(T)).ToList();
                // SharpGenConsole.PrintInfoLine("usedTypes: " + String.Join(",", usedTypeNames));

                // Filter SyntaxTrees to trees that define a used Type, otherwise the tree is not needed in this compilation
                compilationTrees = sourceSyntaxTrees.Where(SST => SyntaxTreeDefinesUsedType(compilation, SST.SyntaxTree, usedTypeNames))
                                                    .Select(SST => SST.SyntaxTree)
                                                    .ToList();

                // Removed unused Using statements from the additional entrypoint source
                List<string> usedNamespaceNames = GetUsedTypes(compilation, sourceTree)
                    .Select(T => GetFullyQualifiedContainingNamespaceName(T)).Distinct().ToList();
                // SharpGenConsole.PrintInfoLine("usedNamespaces: " + String.Join(",", usedNamespaceNames));
                List<SyntaxNode> unusedUsingDirectives = sourceTree.GetRoot().DescendantNodes().Where(N =>
                {
                    return N.Kind() == SyntaxKind.UsingDirective && !usedNamespaceNames.Contains(((UsingDirectiveSyntax)N).Name.ToFullString());
                }).ToList();
                sourceTree = sourceTree.GetRoot().RemoveNodes(unusedUsingDirectives, SyntaxRemoveOptions.KeepNoTrivia).SyntaxTree;

                // Compile again, with unused SyntaxTrees and unused using statements removed
                SharpGenConsole.PrintFormattedProgressLine("Compiling optimized source: ");
                SharpGenConsole.PrintInfoLine(sourceTree.ToString());
                compilationTrees.Add(sourceTree);
                compilation = CSharpCompilation.Create(
                    request.AssemblyName == null ? Path.GetRandomFileName() : request.AssemblyName,
                    compilationTrees,
                    request.References.Where(R => R.Framework == request.TargetDotNetVersion).Where(R => R.Enabled).Select(R =>
                    {
                        string folder = (request.TargetDotNetVersion == DotNetVersion.Net35 ? request.ReferenceDirectory + "net35" + Path.DirectorySeparatorChar : request.ReferenceDirectory + "net40" + Path.DirectorySeparatorChar);
                        return MetadataReference.CreateFromFile(folder + R.File);
                    }).ToList(),
                    options
                );
            }

            // Emit compilation
            EmitResult emitResult;
            byte[] ILbytes = null;
            using (var ms = new MemoryStream())
            {
                emitResult = compilation.Emit(
                    ms,
                    manifestResources: request.EmbeddedResources.Where(ER =>
                    {
                        return request.Platform == Platform.AnyCpu || ER.Platform == Platform.AnyCpu || ER.Platform == request.Platform;
                    }).Where(ER => ER.Enabled).Select(ER =>
                    {
                    return new ResourceDescription(ER.Name, () => File.OpenRead(request.ResourceDirectory + ER.File), true);
                    }).ToList()
                );
                if (emitResult.Success)
                {
                    ms.Flush();
                    ms.Seek(0, SeekOrigin.Begin);
                    ILbytes = ms.ToArray();
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (Diagnostic d in emitResult.Diagnostics)
                    {
                        sb.AppendLine(d.ToString());
                    }
                    throw new CompilerException("CompilationErrors: " + Environment.NewLine + sb);
                }
            }
            return ILbytes;
        }

        private static string GetFullyQualifiedContainingNamespaceName(INamespaceSymbol namespaceSymbol)
        {
            string name = namespaceSymbol.Name;
            namespaceSymbol = namespaceSymbol.ContainingNamespace;
            while (namespaceSymbol != null)
            {
                name = namespaceSymbol.Name + "." + name;
                namespaceSymbol = namespaceSymbol.ContainingNamespace;
            }
            return name.Trim('.');
        }

        private static string GetFullyQualifiedContainingNamespaceName(INamedTypeSymbol namedTypeSymbol)
        {
            return GetFullyQualifiedContainingNamespaceName(namedTypeSymbol.ContainingNamespace);
        }

        private static string GetFullyQualifiedTypeName(INamedTypeSymbol namedTypeSymbol)
        {
            return GetFullyQualifiedContainingNamespaceName(namedTypeSymbol) + "." + namedTypeSymbol.Name;
        }

        private static bool SyntaxTreeDefinesUsedType(CSharpCompilation compilation, SyntaxTree tree, List<string> typeNames)
        {
            SemanticModel model = compilation.GetSemanticModel(tree);
            return null != tree.GetRoot().DescendantNodes().FirstOrDefault(SN =>
            {
                if (SN.Kind() != SyntaxKind.ClassDeclaration)
                {
                    return false;
                }
                INamedTypeSymbol symbol = model.GetDeclaredSymbol(((ClassDeclarationSyntax)SN));
                if (symbol == null)
                {
                    return false;
                }
                return typeNames.Contains(
                             GetFullyQualifiedTypeName(symbol)
                       );
            });
        }

        private static List<INamedTypeSymbol> GetUsedTypes(CSharpCompilation compilation, SyntaxTree sourceTree)
        {
            return sourceTree.GetRoot().DescendantNodes().Select(N =>
            {
                ISymbol symbol = compilation.GetSemanticModel(sourceTree).GetSymbolInfo(N).Symbol;
                if (symbol != null && symbol.ContainingType != null)
                {
                    return symbol.ContainingType;
                }
                return null;
            }).Distinct().Where(T => T != null).ToList();
        }

        private static List<INamedTypeSymbol> GetUsedTypesRecursively(CSharpCompilation compilation, SyntaxTree sourceTree, ref List<INamedTypeSymbol> currentUsedTypes, ref List<SourceSyntaxTree> sourceSyntaxTrees)
        {
            List<string> copyCurrentUsedTypes = currentUsedTypes.Select(CT => GetFullyQualifiedTypeName(CT)).ToList();
            List<INamedTypeSymbol> usedTypes = GetUsedTypes(compilation, sourceTree)
                .Where(T => !copyCurrentUsedTypes.Contains(GetFullyQualifiedTypeName(T)))
                .ToList();
            currentUsedTypes.AddRange(usedTypes);

            List<SyntaxTree> searchTrees = new List<SyntaxTree>();
            foreach (INamedTypeSymbol symbol in usedTypes)
            {
                SyntaxReference sr = symbol.DeclaringSyntaxReferences.FirstOrDefault();
                if (sr != null)
                {
                    SourceSyntaxTree sst = sourceSyntaxTrees.FirstOrDefault(SST => SST.SyntaxTree == sr.SyntaxTree);
                    if (sst != null) { sst.UsedTypes.Add(symbol); }
                    string fullyQualifiedTypeName = GetFullyQualifiedTypeName(symbol);
                    searchTrees.Add(sr.SyntaxTree);
                }
            }

            searchTrees = searchTrees.Distinct().ToList();
            foreach (SyntaxTree tree in searchTrees)
            {
                List<INamedTypeSymbol> newTypes = GetUsedTypesRecursively(compilation, tree, ref currentUsedTypes, ref sourceSyntaxTrees);
                currentUsedTypes.AddRange(newTypes);
            }
            return currentUsedTypes;
        }

        public static byte[] Compress(byte[] bytes)
        {
            byte[] compressedILBytes;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionMode.Compress))
                {
                    deflateStream.Write(bytes, 0, bytes.Length);
                }
                compressedILBytes = memoryStream.ToArray();
            }
            return compressedILBytes;
        }
    }

    public class CompilerException : Exception
    {
        public CompilerException()
        {

        }

        public CompilerException(string message) : base(message)
        {

        }

        public CompilerException(string message, Exception inner) : base(message, inner)
        {

        }
    }
}