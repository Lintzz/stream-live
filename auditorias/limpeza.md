# Limpeza — registro
Data: 2026-10-05
Ferramenta: analisadores do Roslyn (IDE0051, IDE0052, IDE0060, IDE0005) habilitados por .editorconfig temporário numa cópia isolada, com EnforceCodeStyleInBuild. O `dotnet format analyzers --verify-no-changes` sozinho NÃO roda essas regras (prova de controle: código morto plantado não foi acusado) — não confiar nele.

## Removido
- Nada

## Pendente (precisa de resposta)
- 64 `using` desnecessários em ~50 arquivos (IDE0005), cobertos pelo ImplicitUsings. Estilo, não código morto; se limpar, um commit `style:` único

## Decidi manter (parece órfão, e não é)
- MainWindow.StreamTab_OnCloseRequested / OnRetryRequested / OnFocusRequested / OnActivated — ligados pelo XAML (MainWindow.xaml:405-408); o analisador do C# não enxerga ligação em XAML
- Assets/app_icon_source_1024.png — sem referência no código, mas é a arte original do ícone (trocar o ícone está nos planos)
