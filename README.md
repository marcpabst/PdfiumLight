# PdfiumLight
A lightweight C# Library to render PDFs with Google's Pdfium in .NET Apps. This is basicly a stripped down version of [Pieter van Ginkel's PdfiumViewer](https://github.com/pvginkel/PdfiumViewer) with added functionality. 

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
