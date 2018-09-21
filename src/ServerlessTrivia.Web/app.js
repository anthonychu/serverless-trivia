//const apiBaseUrl = 'http://localhost:7071'
const apiBaseUrl = 'https://serverless-trivia.azurewebsites.net'
const axiosConfig = {}
const sessionId = uuid.v4()

const data = {
  nextClueTime: new Date(),
  nextClueSecondsRemaining: 0,
  clues: [],
  sessionScore: {
    correct: 0,
    incorrect: 0,
    totalClues: 0
  },
  allSessionScore: {
    correct: 0,
    incorrect: 0
  },
  previous: {
    correctAnswer: '',
    isCorrect: false,
    everyoneCorrect: 0,
    everyoneIncorrect: 0,
    guessed: false
  }
}

const app = new Vue({
  el: '#app',
  data: data,
  methods: {
    submitGuess: function (clue) {
      const guess = {
        sessionId: sessionId,
        clueId: clue.clueId,
        value: clue.guess
      }
      clue.submittingGuess = true
      clue.guessed = true
      return axios.post(`${apiBaseUrl}/api/SubmitGuess`, guess, axiosConfig)
        .then(function (resp) {
          const result = resp.data
          clue.isCorrect = result.isCorrect
          if (clue.isCorrect) {
            data.sessionScore.correct += 1
          } else {
            data.sessionScore.incorrect += 1
          }
        })
        .catch(function () { alert('Error submitting guess') })
        .finally(function () { clue.submittingGuess = false })
    }
  }
})

getConnectionInfo().then(function (info) {
  let accessToken = info.accessToken
  const options = {
    accessTokenFactory: function () {
      if (accessToken) {
        const _accessToken = accessToken
        accessToken = null
        return _accessToken
      } else {
        return getConnectionInfo().then(function (info) {
          return info.accessToken
        })
      }
    }
  }

  const connection = new signalR.HubConnectionBuilder()
    .withUrl(info.url, options)
    .build()

  connection.on('newClue', newClue)
  connection.on('newGuess', newGuess)
  connection.onclose(function () {
    console.log('disconnected')
    setTimeout(function () { startConnection(connection) }, 2000)
  })

  startConnection(connection)

}).catch(console.error)

function newClue(clue) {
  data.currentClue = clue.nextClue

  const previousClue = (data.clues.length && data.clues[0]) || null
  if (previousClue && previousClue.clueId === clue.previousClue.PartitionKey /* yuck */) {
    previousClue.answer = clue.previousClue.answer
    data.previous.correctAnswer = previousClue.answer
    data.previous.everyoneCorrect = previousClue.guesses.correct
    data.previous.everyoneIncorrect = previousClue.guesses.incorrect
    data.previous.isCorrect = previousClue.isCorrect
    data.previous.guessed = previousClue.guessed
  }

  const nextClue = clue.nextClue
  nextClue.guess = ''
  nextClue.guessed = false
  nextClue.guesses = {
    correct: 0,
    incorrect: 0
  }
  data.clues.unshift(nextClue)

  const now = new Date()
  data.nextClueTime = new Date(now.getTime() + (data.currentClue.estimatedTimeRemaining || 0))

  data.sessionScore.totalClues += 1

  if (data.clues.length > 20) {
    data.clues.length = 20
  }
}

function newGuess(guess) {
  const currentClue = (data.clues.length && data.clues[0]) || null
  if (currentClue && currentClue.clueId === guess.clueId) {
    if (guess.isCorrect) {
      currentClue.guesses.correct += 1
      data.allSessionScore.correct += 1
    } else {
      currentClue.guesses.incorrect += 1
      data.allSessionScore.incorrect += 1
    }

  }
}

function startConnection(connection) {
  console.log('connecting...')
  connection.start()
    .then(function () { console.log('connected!') })
    .catch(function (err) {
      console.error(err)
      setTimeout(function () { startConnection(connection) }, 2000)
    })
}

function getConnectionInfo() {
  return axios.post(`${apiBaseUrl}/api/SignalRInfo`, null, axiosConfig)
    .then(function (resp) { return resp.data })
    .catch(function () { return {} })
}

function calculateTimeRemaining() {
  const now = new Date()
  const diff = Math.round((data.nextClueTime.getTime() - now.getTime()) / 1000.0)
  data.nextClueSecondsRemaining = diff > 0 ? diff : 0
}
setInterval(calculateTimeRemaining, 200)