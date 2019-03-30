# ModernPseudoSh
Experiments in a modern Windows console using the new PTY

`Wpfsh/` contains a WPF implementation.
`ConPty/` contains the mechanisms for allocating a console to the WPF application, enabling a ConPTY mode on it, 
and setting up a communication channel between it and the wPF app.

It's very simple and proof-of-concept right now. Currently, it's in a state where you'd want to implement a VT parser, (probably) a tokenizer, and of course, a renderer. 
This is the bare minimum starting point, and should in no way be considered anything other than a learning experiment.
