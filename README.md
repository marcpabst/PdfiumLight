# PdfiumLight
A lightweight C# Library to render PDFs with Google's Pdfium in .NET Apps. This is basically a much slimmer version of [Pieter van Ginkel's PdfiumViewer](https://github.com/pvginkel/PdfiumViewer) with some added functionality. 

## Getting started
Getting started is easy. Just add `PdfiumLight`, `PdfiumViewer.Native.x86.v8-xfa` and / or `PdfiumViewer.Native.x86_64.v8-xfa` (see below) as  NuGet dependencies to your project and you're good to go:
```c#
// Load the pdf file and create a new document object
PdfDocument document = new PdfDocument("C:/Users/Tom/Documents/sample.pdf");
// Load the first page
PdfPage page = document.GetPage(0);
// Render the page
Image renderedPage = page.Render(700, 1200, 1, 1, PdfRotation.Rotate0, PdfRenderFlags.None);
 ```
### You have to provide pdfium.dll
There are many ways to include pdfium.dll, the most easy one is by adding one or both of the following NuGet-dependencies created by @pvginkel:

- `PdfiumViewer.Native.x86.v8-xfa` 
- `PdfiumViewer.Native.x86_64.v8-xfa`

A very basic packages.config could look like this:
```xm
<?xml version="1.0" encoding="utf-8"?>
<packages>
  <package id="PdfiumLight" version="0.0.3" targetFramework="net45" />
  <package id="PdfiumViewer.Native.x86.v8-xfa" version="2018.4.8.256" targetFramework="net45" />
  <package id="PdfiumViewer.Native.x86_64.v8-xfa" version="2018.4.8.256" targetFramework="net45" />
</packages>
```
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
### (wishlist)
- caching
- part-by-part rendering (although I'm not sure if Pdfium does support it)
- simple editing features such as reorder and deleting pages and merging documents
