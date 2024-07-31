package com.xdesign.flink.processing;

import com.xdesign.flink.model.SlackMessage;
import org.apache.flink.api.java.tuple.Tuple2;

import java.time.Instant;
import java.time.ZoneId;
import java.time.format.DateTimeFormatter;
import java.util.ArrayList;
import java.util.List;

public class StanfordSentimentAccumulator {

    private long start;
    private long end;
    private double averageScore;
    private String result;
    private String mostPositiveMessage;
    private String mostNegativeMessage;
    private int messageCount;
    private List<Integer> scores;
    private List<String> classes;

    public StanfordSentimentAccumulator() {
        this.scores = new ArrayList<>();
        this.classes = new ArrayList<>();
        this.messageCount = 0;
    }

    public void add(SlackMessage message, Tuple2<List<Integer>, List<String>> sentiment) {
        this.scores.addAll(sentiment.f0);
        this.classes.addAll(sentiment.f1);
        this.messageCount++;

        if (this.mostPositiveMessage == null || sentiment.f0.get(0) > this.averageScore) {
            this.mostPositiveMessage = message.getMessage();
        }
        if (this.mostNegativeMessage == null || sentiment.f0.get(0) < this.averageScore) {
            this.mostNegativeMessage = message.getMessage();
        }

        // Calculate average score
        this.averageScore = this.scores
                .stream()
                .mapToInt(Integer::intValue)
                .average()
                .orElse(0.0);

        // Set result based on the average score
        this.result = classifySentiment(this.averageScore);
    }

    public void merge(StanfordSentimentAccumulator other) {
        int totalMessages = this.messageCount + other.messageCount;
        double totalScore = (this.averageScore * this.messageCount + other.averageScore * other.messageCount);

        this.scores.addAll(other.scores);
        this.classes.addAll(other.classes);
        this.messageCount = totalMessages;

        this.averageScore = totalScore / totalMessages;

        // Retain the most positive and most negative messages
        if (this.mostPositiveMessage == null || (other.mostPositiveMessage != null && other.averageScore > this.averageScore)) {
            this.mostPositiveMessage = other.mostPositiveMessage;
        }
        if (this.mostNegativeMessage == null || (other.mostNegativeMessage != null && other.averageScore < this.averageScore)) {
            this.mostNegativeMessage = other.mostNegativeMessage;
        }

        // Set result based on the new average score
        this.result = classifySentiment(this.averageScore);
    }

    private String classifySentiment(double averageScore) {
        if (averageScore >= 3.5) {
            return "Very positive";
        } else if (averageScore >= 2.5) {
            return "Positive";
        } else if (averageScore >= 1.5) {
            return "Neutral";
        } else if (averageScore >= 0.5) {
            return "Negative";
        } else {
            return "Very negative";
        }
    }

    @Override
    public String toString() {
        var formatter = DateTimeFormatter.ofPattern("yyyy-MM-dd'T'HH:mm:ss").withZone(ZoneId.systemDefault());
        var startFormatted = formatter.format(Instant.ofEpochMilli(start));
        var endFormatted = formatter.format(Instant.ofEpochMilli(end));

        return String.format("SentimentAccumulator { start=%s, end=%s, averageScore=%.2f, result='%s', mostPositiveMessage='%s', mostNegativeMessage='%s', messageCount=%d }",
                startFormatted, endFormatted, averageScore, result, mostPositiveMessage, mostNegativeMessage, messageCount);
    }

    public Object getStart() {
        return start;
    }

    public Object getEnd() {
        return end;
    }

    public int getMessageCount() {
        return messageCount;
    }

    public double getAverageScore() {
        return averageScore;
    }

    public String getResult() {
        return result;
    }

    public String getMostPositiveMessage() {
        return mostPositiveMessage;
    }

    public String getMostNegativeMessage() {
        return mostNegativeMessage;
    }

    public void setStart(long start) {
        this.start = start;
    }

    public void setEnd(long end) {
        this.end = end;
    }
}