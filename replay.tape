Output replay.mp4
Set Shell "bash"
Set FontSize 14
Set Width 1920
Set Height 1080

Type "nix run github:unhappychoice/gitlogue -- --speed 15 --commit ${GITLOGUE_RANGE:-HEAD}; echo FIN''ISHED"
Enter
Wait+Screen@600s /FINISHED/
Sleep 1s
