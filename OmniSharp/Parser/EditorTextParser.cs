﻿using System;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;
using OmniSharp.Solution;

namespace OmniSharp.Parser
{
    public class EditorTextParser
    {
        private readonly ISolution _solution;

        public EditorTextParser(ISolution solution)
        {
            _solution = solution;
        }

        public ParsedResult ParsedContent(string editorText, string filename)
        {
            IProjectContent pctx;
            var syntaxTree = new CSharpParser().Parse(editorText, filename);
            syntaxTree.Freeze();
            CSharpUnresolvedFile parsedFile = syntaxTree.ToTypeSystem();

            var project = ProjectContainingFile(filename);
            if (project == null)
            {
                // First we know about this file
                //TODO: if the file isn't part of the solution, we need to add the file to an appropriate project
                project = _solution.Projects.First().Value;
                parsedFile = (CSharpUnresolvedFile) new CSharpFile(project, filename, editorText).ParsedFile;
                pctx = project.ProjectContent;
                pctx = pctx.AddOrUpdateFiles(parsedFile);
            }
            else
            {
                pctx = project.ProjectContent;
                IUnresolvedFile oldFile = pctx.GetFile(filename);
                pctx = pctx.AddOrUpdateFiles(oldFile, parsedFile);
            }
            
            project.ProjectContent = pctx;
            ICompilation cmp = pctx.CreateCompilation();

            return new ParsedResult
                {
                    ProjectContent = pctx,
                    Compilation = cmp,
                    UnresolvedFile = parsedFile,
                    SyntaxTree = syntaxTree
                };
        }

        private IProject ProjectContainingFile(string filename)
        {
            return _solution.Projects.Values.FirstOrDefault(p => p.Files.Any(f => f.FileName.Equals(filename, StringComparison.InvariantCultureIgnoreCase)));
        }
    }
}