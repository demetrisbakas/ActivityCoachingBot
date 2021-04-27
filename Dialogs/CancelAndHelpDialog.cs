// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class CancelAndHelpDialog : ComponentDialog
    {
        private const string CancelMsgText = "Cancelling...";

        public CancelAndHelpDialog(string id)
            : base(id)
        {
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            var result = await InterruptAsync(innerDc, cancellationToken);
            if (result != null)
            {
                return result;
            }

            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        private async Task<DialogTurnResult> InterruptAsync(DialogContext innerDc, CancellationToken cancellationToken)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message && innerDc.Context.Activity.Text != null)
            {
                var text = innerDc.Context.Activity.Text.ToLowerInvariant();

                switch (text)
                {
                    case "help":
                    case "?":
                        var helpMessageText = MainDialog.Response.HelpMessage();
                        var helpMessage = MessageFactory.Text(helpMessageText, helpMessageText, InputHints.ExpectingInput);
                        await innerDc.Context.SendActivityAsync(helpMessage, cancellationToken);
                        return new DialogTurnResult(DialogTurnStatus.Waiting);

                    case "exit":
                    case "cancel":
                    case "quit":
                        var cancelMessage = MessageFactory.Text(CancelMsgText, CancelMsgText, InputHints.IgnoringInput);
                        await innerDc.Context.SendActivityAsync(cancelMessage, cancellationToken);
                        return await innerDc.CancelAllDialogsAsync(cancellationToken);
                }

                if (Regex.IsMatch(text, "exit", RegexOptions.IgnoreCase) || Regex.IsMatch(text, "cancel", RegexOptions.IgnoreCase) || Regex.IsMatch(text, "quit", RegexOptions.IgnoreCase))
                {
                    var cancelMessage = MessageFactory.Text(CancelMsgText, CancelMsgText, InputHints.IgnoringInput);
                    await innerDc.Context.SendActivityAsync(cancelMessage, cancellationToken);
                    return await innerDc.CancelAllDialogsAsync(cancellationToken);
                }

                if (Regex.IsMatch(text, "help", RegexOptions.IgnoreCase))
                {
                    var helpMessageText = MainDialog.Response.HelpMessage();
                    var helpMessage = MessageFactory.Text(helpMessageText, helpMessageText, InputHints.ExpectingInput);
                    await innerDc.Context.SendActivityAsync(helpMessage, cancellationToken);
                    return new DialogTurnResult(DialogTurnStatus.Waiting);
                }
            }

            return null;
        }
    }
}
