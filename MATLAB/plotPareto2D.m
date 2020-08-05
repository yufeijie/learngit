function plotPareto2D(titleName, xLabelName, yLabelName, size, data, dataPareto, nMin, nMax)
% plotPareto2D 画Pareto图（2D，成本-效率）
% titleName 大标题
% xLabelName yLabelName 坐标轴名
% size 数据总数
% data  数据
% dataPareto Pareto最优数据
global sz mkr1 mkr2 Nmin Nmax
sz = 12;
mkr1 = 'o';
mkr2 = 'o';
Nmin = double(nMin);
Nmax = double(nMax);

figure(9)

m = length(dataPareto(:, 1));

ax1 = subplot(2,1,1);
[plot1, plot2] = plotInDiffType(ax1, data(1:size, 1), data(1:size, 3), data(1:size, 4), data(1:size, 8), 0);
for i = 1 : m-1
    for j = i+1 : m
        if dataPareto(i, 3) > dataPareto(j, 3)
            dataPareto([i, j], :) = dataPareto([j, i], :);
        end
    end
end
n = 1;
pareto(1, 1) = dataPareto(1, 1);
pareto(1, 2) = dataPareto(1, 3);
pareto(1, 3) = dataPareto(1, 4);
pareto(1, 4) = dataPareto(1, 8);
for i = 2 : m
    while n > 0 && dataPareto(i, 1) < pareto(n, 1)
        n = n-1;
    end
    n = n+1;
    pareto(n, 1) = dataPareto(i, 1);
    pareto(n, 2) = dataPareto(i, 3);
    pareto(n, 3) = dataPareto(i, 4);
    pareto(n, 4) = dataPareto(i, 8);
end

x1 = zeros(n, 1);
y1 = zeros(n, 1);
c1 = zeros(n, 1);
len1 = 0;
x2 = zeros(n, 1);
y2 = zeros(n, 1);
c2 = zeros(n, 1);
len2 = 0;
for i = 1 : n
    if pareto(i, 4) == 1
        len1 = len1+1;
        x1(len1) = pareto(i, 1);
        y1(len1) = pareto(i, 2);
        c1(len1) = pareto(i, 3);
    else
        len2 = len2+1;
        x2(len2) = pareto(i, 1);
        y2(len2) = pareto(i, 2);
        c2(len2) = pareto(i, 3);
    end
end
plot3 = scatter(ax1, x1(1:len1), y1(1:len1), sz, getCmap(c1(1:len1), 1), mkr1, 'filled');
hold on
plot4 = scatter(ax1, x2(1:len2), y2(1:len2), sz, getCmap(c2(1:len2), 0), mkr2, 'filled');
hold on
plot3.LineWidth = 0.5;
plot3.MarkerEdgeColor = 'r';
plot4.LineWidth = 0.5;
plot4.MarkerEdgeColor = 'r';
plot5 = plot(pareto(1:n, 1), pareto(1:n, 2), 'r');
grid on
%title(titleName)
xlabel(xLabelName)
ylabel(yLabelName)

ax2 = subplot(2,1,2);
colorbar(ax1, 'south', 'Ticks', linspace(0, 1, 8), 'TickLabels', {'30', '40', '50', '60', '70', '80', '90', '100'});
c = colorbar(ax2, 'south', 'Ticks', []);
c.Label.String = 'Module number';
legend(ax1, [plot1, plot2, plot3, plot5], {'Feasible design based on SiC', 'Feasible design based on IGBT', 'Pareto optimal set', 'Pareto front'}, 'Location', 'east');
%legend('boxoff')

%suptitle(headline)
end

function [plot1, plot2] = plotInDiffType(ax, x, y, c, type, pattern)
global sz mkr1 mkr2
n = length(x);
x1 = zeros(n, 1);
y1 = zeros(n, 1);
c1 = zeros(n, 1);
len1 = 0;
x2 = zeros(n, 1);
y2 = zeros(n, 1);
c2 = zeros(n, 1);
len2 = 0;
for i = 1 : n
    if type(i) == 1
        len1 = len1+1;
        x1(len1) = x(i);
        y1(len1) = y(i);
        c1(len1) = c(i);
    else
        len2 = len2+1;
        x2(len2) = x(i);
        y2(len2) = y(i);
        c2(len2) = c(i);
    end
end
plot1 = scatter(ax, x1(1:len1), y1(1:len1), sz, getCmap(c1(1:len1), 1), mkr1, 'filled');
hold on
plot2 = scatter(ax, x2(1:len2), y2(1:len2), sz, getCmap(c2(1:len2), 0), mkr2, 'filled');
hold on
if pattern == 1
    plot1.LineWidth = 0.5;
    plot1.MarkerEdgeColor = 'r';
    plot2.LineWidth = 0.5;
    plot2.MarkerEdgeColor = 'r';
end
end

function cmap = getCmap(c, type)
global Nmin Nmax
shift = 0.4;
n = length(c);
cmap = zeros(n, 3);
for i = 1 : n
    if type == 1
        normal = (c(i)-Nmin)/(Nmax-Nmin);
        delta = (1-shift)*normal;
        cmap(i, 1) = delta;
        cmap(i, 2) = delta;
        cmap(i, 3) = shift+delta;
    else
        normal = (c(i)-Nmin)/(Nmax-Nmin);
        delta = (1-shift)*normal;
        cmap(i, 1) = delta;
        cmap(i, 2) = shift+delta;
        cmap(i, 3) = delta;
    end
end
end