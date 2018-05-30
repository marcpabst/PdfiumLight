# PdfiumLight
A lightweight C# Library to render PDFs with Google's Pdfium in .NET Apps. This is basicly a stripped down version of [Pieter van Ginkel's PdfiumViewer](https://github.com/pvginkel/PdfiumViewer) with added functionality. 

## Getting started
Getting started is easy. Just add this as a Nuget dependency and you're good to go:
```c#
// Load the pdf file and create a new document object
var document = new PdfDocument("C:/Users/Marc/Documents/sample.pdf");
// Load the first page
var page = document.GetPage(0);
// Render the page
var renderedPage = page.Render(1000, 2500, 1, 1, PdfRotation.Rotate0, PdfRenderFlags.None);
 ```
 Please refer to the "Getting started" section in the wiki for more informaton.

## Features
### (already implemented)
- load PDF-documents
- render pages
- convert device coordinates to page coordinates and vice versa
- extract text from page
- check if there is text at a specific point on the page  (very usefull if implementing a text layer)
### (still to come)
- support for internal and external links
- search
- annoatation support
- form support
### (wishlist, ordered by importance)
- caching
- part-by-part rendering (although I'm not sure if Pdfium does support it)
- simple editing features such as reorder and deleting pages and merging documents
