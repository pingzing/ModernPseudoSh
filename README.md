# ModernPseudoSh
Experiments in a modern Windows console using the new PTY

Features a WPF and a UWP project, both in an extremely messy state, with lots of duplication.

It's probably possible to pull all the Console stuff out into a library, and share it everywhere.

The UWP project is a little more complete, and is in a state where you could conceivably start implementing a VT parser and renderer.
Hard parts would include a Windows Scancode -> VT (and vice versa) mapping.
