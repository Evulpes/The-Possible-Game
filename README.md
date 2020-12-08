# The-Possible-Game
## Introduction - It's 2009
As a wise man once said, “Nothing is impossible, except this game”, and that man was almost right. However, what said man forgot was that [NOP sliding](https://en.wikipedia.org/wiki/NOP_slide) your way to victory is actually indeed a valid strategy:

### Video:
![gif](https://github.com/Evulpes/The-Possible-Game/blob/master/IGV.gif?raw=true)

For those who aren’t familiar with [The Impossible Game](https://impossible.game/), it’s a simple platformer that was originally released on the Apple App Store in 2009, and later made its way to PC in 2014 via Steam. It’s really, really, annoying. 

## Ante Up for Anti-Cheat
First things first, let’s attach a debugger the main executable and launch it. I’m using [x64dbg](https://x64dbg.com/#start), but [other](https://docs.microsoft.com/en-us/windows-hardware/drivers/debugger/debugger-download-tools) debuggers are commercially available. 
Unfortunately, we’ve hit an obstacle faster than Usain Bolt would if he were blind; it appears my uncle Gabe over at Valve has decided to roll out some basic protection to all Steam games in the past few months, as there’s now some anti-debugging where there wasn’t a few months ago (citation needed):

<p align="center">
  <img src="https://i.imgur.com/ln76q54.png"/>
</p> 

So, unless your debugger is equipped with the [secret hacker tech](https://github.com/x64dbg/ScyllaHide) to automatically bypass this check, you’re going to have defeat the check manually! Fortunately, this is a great opportunity to go over how you can achieve this without the need for third party tools.
However, before all of that, we first need some simple code to open a handle to the process and find some space to write our custom assembly code into the target process. Firstly, we’re going to assume that the process is already running and just straight up ask for a handle. Who is the process to tell me no?

<p align="center">
  <img src="https://i.imgur.com/8X4mT9i.png"/>
</p> 

Next, we need to write the assembly code to defeat the anti-debugging technique used here. As a reminder, this is the function that’s causing issues:

<p align="center">
  <img src="https://i.imgur.com/ln76q54.png"/>
</p> 

The IsDebuggerPresent function checks the [Process Environment Block](https://docs.microsoft.com/en-us/windows/win32/api/winternl/ns-winternl-peb) for a flag called BeingDebugged. The first line of the above function is moving the address of the PEB into eax using the FS segment, the second line is moving the flag, at offset 0x2, into eax, and the third line is returning it. With this information in mind, we can write our own little loop to get and set this byte. Here’s some assembly code I made earlier:

<p align="center">
  <img src="https://i.imgur.com/bUkS2Xs.png"/>
</p> 

The pseudo code for this is:
1.	Move the address of the PEB into eax
2.	Check if the byte at eax+0x2, the BeingDebugged flag, is 0x1 (True)
3.	If the byte is not 0x1 (True), then jump back to step 1
4.	If the byte is 0x1 (True), then set it to 0x0 (False)
5.	Jump back to step 1

We’re now mom-spaghetti-ready to write the code into the target process. We can call VirtualAllocEx and set the second parameter as IntPtr.Zero, which will allow the function itself to choose a suitable location for us to write our assembly code to, without causing issues. We’ll then use WriteProcessMemory to “install our update”, obviously:

<p align="center">
  <img src="https://i.imgur.com/siVvaOy.png"/>
</p> 

Here’s the result in the disassembly. Looking good!

<p align="center">
  <img src="https://i.imgur.com/wSZfV7t.png"/>
</p> 

Now all we need to do is start a remote thread that will run our code:

<p align="center">
  <img src="https://i.imgur.com/shpw83b.png"/>
</p> 

It’s important to note that this thread will never actually terminate and will just run endlessly within the target application. You can read more about how to use this function [here](https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-createremotethread), but we’re effectively just giving it the address of the first instruction and telling it to go-go gadget.

## Let’s Debug It Like They Debug It on the I/O Channel
Cool, now that we’ve circumvented the very simple anti-debugging protections, we can look at identifying how to cheese this game. You’ll obviously need to spin up the game and apply the patch we’ve just written to do any debugging.

Now, it is in my very valuable opinion that the easiest way to cheese this game is going to be to simply remove any concept of “death” from the game, preventing the physics interactions with the black objects, and simply “ghosting” through them. Fortunately, [Grip Digital](https://en.wikipedia.org/wiki/Grip_Digital) decided it would be a great convenience to them if they added debugging messages to the game. Highlighted are two potentially useful strings that we can likely track and trace:

<p align="center">
  <img src="https://i.imgur.com/j48dgoT.png"/>
</p> 

The first one, “RESET”, is immediately thrown when the player encounters an obstacle, or “dies”, and the second is shortly after when the game over menu appears. If we do a search for some cheese strings, we can find these strings, and where they are used within the disassembly:

<p align="center">
  <img src="https://i.imgur.com/ZB1cGYL.png"/>
</p> 
<p align="center">
  <img src="https://i.imgur.com/7gDK48x.png"/>
</p> 

So, what happens if we put a breakpoint at the first string? Well, as expected, the game immediately freezes and stops  at the breakpoint, just as the player hits an object. Thereby, it’s a safe assumption to say that this is the death function, but we can verify this.

## What Do We Say to The God of Death?
Let’s pretend that death is a door-to-door salesman and we’d just rather not. By changing the first line of the function call to 0xC3 ([ret](https://c9x.me/x86/html/file_module_x86_id_280.html)), we’re simply telling the function we’re not interested in its content to go back from whence it came (before and after):

<p align="center">
  <img src="https://i.imgur.com/ka8smIf.png"/>
</p> 

And when running this modified code, the game decides it really isn’t interested in doing its job and just wants to [cha-cha slide](https://youtu.be/wZv62ShoStY) the player across the map with absolutely no consequences whatsoever:

<p align="center">
  <img src="https://i.imgur.com/vgJo034.png"/>
</p> 

As a working method, this is fine; we could write a program that simply writes 0xC3 to base address + offset of this function, but if the developers decide that they want to update the game one day there’s a good chance the offset would change. For longevity purposes, it’s going to be better to scan for the function and NOP it out. Luckily, there’s only one reference to this function, and thereby only one set of bytes to scan for:

<p align="center">
  <img src="https://i.imgur.com/m2Pr7vp.png"/>
</p> 

<p align="center">
  <img src="https://i.imgur.com/oEAAWYn.png"/>
</p> 

Coding time!

## NOP Slide to The Left
Right, so here’s the NOP slide we’re going to apply, and the bytes we’re looking for:

<p align="center">
  <img src="https://i.imgur.com/oQ1oxvZ.png"/>
</p> 

Assuming the anti-debugging patch applies with no problems, we can open a handle to the process and scan for the function bytes. I won’t go over the byte scanning code here, but essentially, we’re dumping the entirety of the main executable into a byte array, checking for a matching pattern, and returning the address of the pattern, if found. You can review the code for this [here](https://github.com/Evulpes/Generic-Bytescan-Library) (although I’d recommend you look for a better library, as mine has some flaws, or just use the static offset).

<p align="center">
  <img src="https://i.imgur.com/5w4CVuB.png"/>
</p> 

We can then write our NOP slide to this address, overwriting the death function, and we’re done:

<p align="center">
  <img src="https://i.imgur.com/e82ciGX.png"/>
</p> 

<p align="center">
  <img src="https://i.imgur.com/m4Eh9BG.png"/>
</p> 

If you plan to extend this code for any reason, don’t forget to clean up the handles properly!
