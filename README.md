# MiniLogicA: a *Mini*mizer and a companion for Vox*LogicA*

MiniLogicA is a minimizer for images and graphs. It quotients such spatial structures according to logical equivalence of the Spatial Logic of Closure Spaces SLCS (see https://lmcs.episciences.org/2067/pdf). It aims to be a companion for the spatial model checker VoxLogicA (see http://www.voxlogica.com) even if, right now, it just computes the quotient. 

Source code requires `dotnet core 3.0` to compile, see https://dotnet.microsoft.com/download. Run "make publish" to create the executables.

*Download link:* https://github.com/vincenzoml/MiniLogicA/releases

## Purpose 

Given either a graph or an image, the tool will compute 1) the minimal system in the form of a graph; 2) the equivalence classes (textual representation); 3) a coloured copy of the original system (either image or graph) with colours corresponding to the equivalence classes; 4) furthermore, when the input is an image, output #1, although being a graph, will have the same colours of the original image

## Current state 

Only #1 is implemented. The computation of #2 and #3 is already in the source code, but saving is not implemented yet. #4 is not implemented.

## Input and output formats for images and graphs

#### Input, graphs

The chosen input format is a custom format in the json standard notation (http://www.json.org/). There is a graph in the source tree. The json format should be self-explanatory. All identifiers (nodes, atomic propositions) are strings, so they can be anything (including spaces and symbols, but please don't, it will mess-up graphviz).

#### Input, images 

Currently, the tool can load 2-dimensional png images.


#### Output, graphs 

Currently, only .dot is supported; json would be trivial but the code is not there yet. Files with .dot extension can be opened with graphviz.

## Usage

If only one argument is supplied, the graph is printed on standard output. This makes pipelining possible, e.g. "MinLogicA input.png | neato -Tx11". If two arguments are supplied, the graph is saved to the given output file. If wrong arguments are passed the program will print usage information. Exceptions are not handled yet, so any error (e.g. wrong format in input file, or missing input file) will result in very ugly messages. 

