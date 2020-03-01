[Saturn Framework]: https://saturnframework.org/
[SAFE Template]: https://safe-stack.github.io/docs/template-heroku/

# Palabrator API
Few years ago I made a Nativescript Mobile application for my son when he was about two years old. The app was pretty simple, it was just a simple slideshow of fruits with a rotative animation when clicked the fruit bounced and made a noise...

God did my son loved that... I felt so joyful and proud of my lame attempt of an app. The biggest limitation was that I couldn't simply just add more words/pictures unless I manually deployed to the mobile device. Now that another baby is coming I inted to continue that project with some improvements and more customizations.

Enter Palabrator API, this is just a hobby project that uses [Saturn Framework] and is hosted somewhere in Heroku.

If you like the way I set this up you have to know that I just used a [SAFE Template] and stripped out the clientside

```
dotnet new SAFE --deploy heroku -o Palabrator
```
then I proceded to delete client side assets as well as to update the paket dependencies and build.fsx
