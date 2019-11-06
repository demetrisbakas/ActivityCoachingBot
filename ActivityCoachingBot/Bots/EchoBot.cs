// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.5.0

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder.AI.Luis;

namespace ActivityCoachingBot.Bots
{
    public class EchoBot : ActivityHandler
    {
        private LuisRecognizer Recognizer { get; } = null;
        //private readonly EchoBotAccessors _accessors;
        //private DialogSet _dialogs;




        //public EchoBot(EchoBotAccessors accessors, LuisRecognizer luis)
        //{
        //    // The incoming luis variable is the LUIS Recognizer we added above.
        //    this.Recognizer = luis ?? throw new System.ArgumentNullException(nameof(luis));

        //    // Set the _accessors 
        //    //_accessors = accessors ?? throw new System.ArgumentNullException(nameof(accessors));

        //    // The DialogSet needs a DialogState accessor, it will call it when it has a turn context.
        //    //_dialogs = new DialogSet(accessors.ConversationDialogState)
        //}






        //public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        //{
        //    if (turnContext.Activity.Type == ActivityTypes.Message)
        //    {
        //        // Check LUIS model
        //        var recognizerResult = await this.Recognizer.RecognizeAsync(turnContext, cancellationToken);
        //        var topIntent = recognizerResult?.GetTopScoringIntent();

        //        // Get the Intent as a string
        //        string strIntent = (topIntent != null) ? topIntent.Value.intent : "";

        //        // Get the IntentScore as a double
        //        double dblIntentScore = (topIntent != null) ? topIntent.Value.score : 0.0;

        //        // Only proceed with LUIS if there is an Intent 
        //        // and the score for the Intent is greater than 95
        //        if (strIntent != "" && (dblIntentScore > 0.95))
        //        {
        //            switch (strIntent)
        //            {
        //                case "None":
        //                    await turnContext.SendActivityAsync("Sorry, I don't understand.");
        //                    break;

        //                case " Greet":
        //                    await turnContext.SendActivityAsync("Hello, how can I help you?");
        //                    break;

        //                default:
        //                    // Received an intent we didn't expect, so send its name and score.
        //                    await turnContext.SendActivityAsync(
        //                        $"Intent: {topIntent.Value.intent} ({topIntent.Value.score}).");
        //                    break;
        //            }
        //        }
        //        else
        //        {
        //            //// Get the conversation state from the turn context.
        //            //var state = await _accessors.CounterState.GetAsync(turnContext, () => new CounterState());

        //            //// Bump the turn count for this conversation.
        //            //state.TurnCount++;

        //            //if (!state.SaidHello)
        //            //{
        //            //    // Handle the Greeting
        //            //    string strMessage = $"Hello World! {System.Environment.NewLine}";
        //            //    await turnContext.SendActivityAsync(strMessage);

        //            //    // Set SaidHello
        //            //    state.SaidHello = true;
        //            //}
        //            //else
        //            //{
        //            //    // Get the user state from the turn context.
        //            //    var user = await _accessors.UserProfile.GetAsync(turnContext, () => new UserProfile());
        //            //    if (user.Name == null)
        //            //    {
        //            //        // Run the DialogSet - let the framework identify the current state of the 
        //            //        // dialog from the dialog stack and figure out what (if any) is the active dialog.
        //            //        var dialogContext = await _dialogs.CreateContextAsync(turnContext, cancellationToken);
        //            //        var results = await dialogContext.ContinueDialogAsync(cancellationToken);

        //            //        // If the DialogTurnStatus is Empty we should start a new dialog.
        //            //        if (results.Status == DialogTurnStatus.Empty)
        //            //        {
        //            //            await dialogContext.BeginDialogAsync("details", null, cancellationToken);
        //            //        }
        //            //    }
        //            //    else
        //            //    {
        //            //        // Echo back to the user whatever they typed.
        //            //        var responseMessage = $"Turn {state.TurnCount}: {user.Name} you said '{turnContext.Activity.Text}'\n";
        //            //        await turnContext.SendActivityAsync(responseMessage);
        //            //    }
        //            //}

        //            //// Set the property using the accessor.
        //            //await _accessors.CounterState.SetAsync(turnContext, state);
        //            //// Save the new turn count into the conversation state.
        //            //await _accessors.ConversationState.SaveChangesAsync(turnContext);
        //            //// Save the user profile updates into the user state.
        //            //await _accessors.UserState.SaveChangesAsync(turnContext, false, cancellationToken);
        //        }
        //    }
        //}







        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync(MessageFactory.Text($"Echo: {turnContext.Activity.Text}"), cancellationToken);



            ////Check LUIS model
            //var recognizerResult = await this.Recognizer.RecognizeAsync(turnContext, cancellationToken);
            //var topIntent = recognizerResult?.GetTopScoringIntent();

            //// Get the Intent as a string
            //string strIntent = (topIntent != null) ? topIntent.Value.intent : "";

            //// Get the IntentScore as a double
            //double dblIntentScore = (topIntent != null) ? topIntent.Value.score : 0.0;

            //// Only proceed with LUIS if there is an Intent 
            //// and the score for the Intent is greater than 95
            //if (strIntent != "" && (dblIntentScore > 0.95))
            //{
            //    switch (strIntent)
            //    {
            //        case "None":
            //            await turnContext.SendActivityAsync("Sorry, I don't understand.");
            //            break;

            //        case " Greet":
            //            await turnContext.SendActivityAsync("Hello, how can I help you?");
            //            break;

            //        default:
            //            // Received an intent we didn't expect, so send its name and score.
            //            await turnContext.SendActivityAsync(
            //                $"Intent: {topIntent.Value.intent} ({topIntent.Value.score}).");
            //            break;
            //    }
            //}
            //else
            //{
            //    await turnContext.SendActivityAsync("No intend.");
            //}















            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                //// Get the user state from the turn context.
                //user = await _accessors.UserProfile.GetAsync(turnContext, () => new WeatherProfile());

                //// Get the conversation state from the turn context.
                //var state = await _accessors.CounterState.GetAsync(turnContext, () => new CounterState());

                // Check LUIS model
                var recognizerResult = await this.Recognizer.RecognizeAsync(turnContext, cancellationToken);
                var topIntent = recognizerResult?.GetTopScoringIntent();

                // Get the Intent as a string
                string strIntent = (topIntent != null) ? topIntent.Value.intent : "";

                // Get the IntentScore as a double
                double dblIntentScore = (topIntent != null) ? topIntent.Value.score : 0.0;

                // Only proceed with LUIS if there is an Intent 
                // and the score for the Intent is greater than 95
                if (strIntent != "" && (dblIntentScore > 0.95))
                {
                    switch (strIntent)
                    {
                        case "None":
                            await turnContext.SendActivityAsync("Sorry, I don't understand.");
                            break;

                        case "Greet":
                            await turnContext.SendActivityAsync("Hello, how can I help you?");
                            break;

                        default:
                            // Received an intent we didn't expect, so send its name and score.
                            await turnContext.SendActivityAsync(
                                $"Intent: {topIntent.Value.intent} ({topIntent.Value.score}).");
                            break;
                    }
                }
                else
                {
                    await turnContext.SendActivityAsync("No intend.");
                }


                

                //// Set the property using the accessor.
                //await _accessors.CounterState.SetAsync(turnContext, state);

                //// Save the new turn count into the conversation state.
                //await _accessors.ConversationState.SaveChangesAsync(turnContext);

                //// Save the user profile updates into the user state.
                //await _accessors.UserState.SaveChangesAsync(turnContext, false, cancellationToken);
            }



        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Hello and welcome!"), cancellationToken);
                }
            }
        }
    }
}
