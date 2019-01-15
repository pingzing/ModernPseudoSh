# ModernPseudoSh
Experiments in a modern Windows console using the new PTY

`Wpfsh/` contains a WPF implementation.

`ConPty/` contains most of the plumbing to get a Windows PTY up and running.

It's very simple and proof-of-concept right now. Currently, it's in a state where you'd want to implement a VT parser, (probably) a tokenizer, and of course, a renderer.
